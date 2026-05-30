using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AffiliateApplicationDTO;
using MuuqWear.Model.DTO.PartnerStoreProductDTO;
using MuuqWear.Model.Models.AffiliateApplication;
using MuuqWear.Model.Models.Order;
using MuuqWear.Model.Models.Product;
using MuuqWear.Model.Models.Profiles;

namespace MuuqWear.Application.Service;

public class AffiliateService : IAffiliateService
{
    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;
    private const int MONTHLY_PURCHASE_LIMIT = 20;
    private const decimal AFFILIATE_DISCOUNT_PERCENTAGE = 0.25m;

    public AffiliateService(
        SupabaseClientFactory factory,
        SupabaseAdminClientFactory adminFactory)
    {
        _client = factory.CreateClient();
        _adminClient = adminFactory.CreateClient();
    }

    // =============================================
    // SUBMIT APPLICATION
    // =============================================
    public async Task<Response<AffiliateApplicationDTO>> SubmitApplication(
        Guid userId, SubmitAffiliateApplicationDTO request)
    {
        try
        {
            // Step 1: Validate input
            var validation = ValidateApplicationRequest(request);
            if (!validation.Success)
                return Response<AffiliateApplicationDTO>.Fail(validation.Message);

            // Step 2: Check if user already applied
            var existingCheck = await _client
                .From<AffiliateApplication>()
                .Where(a => a.UserId == userId)
                .Get();

            if (existingCheck.Models.Any())
                return Response<AffiliateApplicationDTO>.Fail(
                    "You have already submitted an application");

            // Step 3: Check affiliate limit (500 members)
            var approvedCount = await GetApprovedAffiliateCount();

            if (approvedCount >= 500)
            {
                // Auto-waitlist if limit reached
                return await SubmitAsWaitlisted(userId, request);
            }

            // Step 4: Create application
            var application = new AffiliateApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FullName = request.FullName,  //  ADD
                Email = request.Email,
                SocialHandles = request.SocialHandles,
                AudienceSize = request.AudienceSize,
                ContentNiche = request.ContentNiche,
                PortfolioUrl = request.PortfolioUrl,
                WhyMuuqwear = request.WhyMuuqwear,  //  ADD
                SampleFiles = request.SampleFiles,  //  ADD
                Status = "pending",
                SubmittedAt = DateTime.UtcNow
            };

            var insertResult = await _client
                .From<AffiliateApplication>()
                .Insert(application);

            var inserted = insertResult.Models.FirstOrDefault();

            if (inserted == null)
                return Response<AffiliateApplicationDTO>.Fail(
                    "Failed to submit application");

            // Step 5: Update user's profile status
            await UpdateProfileApplicationStatus(userId, "pending");

            // Step 6: Map to DTO
            var dto = MapToDTO(inserted);

            return Response<AffiliateApplicationDTO>.SuccessResponse(
                dto, "Application submitted successfully. You'll be notified once reviewed.");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationDTO>.Fail(
                "Error submitting application: " + ex.Message);
        }
    }

    // =============================================
    // GET USER AFFILIATE STATUS
    // =============================================
    public async Task<Response<AffiliateStatusDTO>> GetUserAffiliateStatus(Guid userId)
    {
        try
        {
            // Get user profile
            var profileResult = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            if (profileResult == null)
                return Response<AffiliateStatusDTO>.Fail("Profile not found");

            // Get application if exists
            var applicationResult = await _client
                .From<AffiliateApplication>()
                .Where(a => a.UserId == userId)
                .Get();

            var application = applicationResult.Models.FirstOrDefault();

            var status = new AffiliateStatusDTO
            {
                ApplicationStatus = profileResult.AffiliateApplicationStatus ?? "not_applied",
                Tier = profileResult.AffiliateTier ?? "none",
                ItemsSold = profileResult.AffiliateItemsSold,
                CommissionEarned = profileResult.AffiliateCommissionEarned,
                SubmittedAt = application?.SubmittedAt
            };

            return Response<AffiliateStatusDTO>.SuccessResponse(status);
        }
        catch (Exception ex)
        {
            return Response<AffiliateStatusDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET USER APPLICATION
    // =============================================
    public async Task<Response<AffiliateApplicationDTO?>> GetUserApplication(Guid userId)
    {
        try
        {
            var result = await _client
                .From<AffiliateApplication>()
                .Where(a => a.UserId == userId)
                .Get();

            var application = result.Models.FirstOrDefault();

            if (application == null)
                return Response<AffiliateApplicationDTO?>.SuccessResponse(
                    null, "No application found");

            var dto = MapToDTO(application);

            return Response<AffiliateApplicationDTO?>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationDTO?>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ALL APPLICATIONS (ADMIN)
    // =============================================
    public async Task<Response<List<AffiliateApplicationDTO>>> GetAllApplications(
      string? statusFilter = null)
    {
        try
        {
            List<AffiliateApplication> applications;

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter))
            {
                var filteredResult = await _adminClient
                    .From<AffiliateApplication>()
                    .Where(a => a.Status == statusFilter)
                    .Order(a => a.SubmittedAt,
                        Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                applications = filteredResult.Models;
            }
            else
            {
                var allResult = await _adminClient
                    .From<AffiliateApplication>()
                    .Order(a => a.SubmittedAt,
                        Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                applications = allResult.Models;
            }

            var dtos = applications.Select(MapToDTO).ToList();

            return Response<List<AffiliateApplicationDTO>>.SuccessResponse(
                dtos, "Applications fetched successfully");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliateApplicationDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE APPLICATION STATUS (ADMIN)
    // =============================================
    public async Task<Response<AffiliateApplicationDTO>> UpdateApplicationStatus(
        Guid applicationId, string status, Guid reviewedBy, string? adminNotes = null)
    {
        try
        {
            // Validate status
            var validStatuses = new[] { "pending", "approved", "rejected", "waitlisted" };
            if (!validStatuses.Contains(status))
                return Response<AffiliateApplicationDTO>.Fail("Invalid status");

            // Get application
            var application = await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.Id == applicationId)
                .Single();

            if (application == null)
                return Response<AffiliateApplicationDTO>.Fail(
                    "Application not found");

            // Update application
            var updated = await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.Id == applicationId)
                .Set(a => a.Status!, status)
                .Set(a => a.ReviewedAt!, DateTime.UtcNow)
                .Set(a => a.ReviewedBy!, reviewedBy)
                .Set(a => a.AdminNotes!, adminNotes ?? "")
                .Update();

            var updatedApp = updated.Models.FirstOrDefault();

            if (updatedApp == null)
                return Response<AffiliateApplicationDTO>.Fail(
                    "Failed to update application");

            // Update user's profile
            await UpdateProfileApplicationStatus(application.UserId, status);

            // If approved, set initial tier
            if (status == "approved")
            {
                await UpdateProfileAffiliateTier(application.UserId, "bronze");
            }

            var dto = MapToDTO(updatedApp);

            return Response<AffiliateApplicationDTO>.SuccessResponse(
                dto, $"Application {status} successfully");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET PENDING COUNT
    // =============================================
    public async Task<Response<int>> GetPendingCount()
    {
        try
        {
            var result = await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.Status == "pending")
                .Get();

            return Response<int>.SuccessResponse(
                result.Models.Count, "Count fetched");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // PRIVATE HELPERS
    // =============================================

    private Response<bool> ValidateApplicationRequest(SubmitAffiliateApplicationDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Response<bool>.Fail("Full name is required");

        // ADD: Email validation
        if (string.IsNullOrWhiteSpace(request.Email))
            return Response<bool>.Fail("Email is required");
        if (!request.SocialHandles.Any())
            return Response<bool>.Fail("At least one social media handle is required");

        if (request.AudienceSize < 100)
            return Response<bool>.Fail("Minimum audience size is 100 followers");

        if (string.IsNullOrWhiteSpace(request.ContentNiche))
            return Response<bool>.Fail("Content niche is required");

        if (request.ContentNiche.Length < 3)
            return Response<bool>.Fail("Content niche must be at least 3 characters");

        // Validate social handles
        foreach (var handle in request.SocialHandles)
        {
            if (string.IsNullOrWhiteSpace(handle.Platform))
                return Response<bool>.Fail("Social platform name is required");

            if (string.IsNullOrWhiteSpace(handle.Handle))
                return Response<bool>.Fail($"{handle.Platform} handle is required");

            if (handle.Followers < 0)
                return Response<bool>.Fail("Follower count cannot be negative");
        }

        return Response<bool>.SuccessResponse(true);
    }

    private async Task<int> GetApprovedAffiliateCount()
    {
        var result = await _adminClient
            .From<Profiles>()
            .Where(p => p.AffiliateTier != "none")
            .Get();

        return result.Models.Count;
    }

    private async Task<Response<AffiliateApplicationDTO>> SubmitAsWaitlisted(
        Guid userId, SubmitAffiliateApplicationDTO request)
    {
        var application = new AffiliateApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SocialHandles = request.SocialHandles,
            AudienceSize = request.AudienceSize,
            ContentNiche = request.ContentNiche,
            PortfolioUrl = request.PortfolioUrl,
            Status = "waitlisted",
            SubmittedAt = DateTime.UtcNow
        };

        var inserted = await _client
            .From<AffiliateApplication>()
            .Insert(application);

        await UpdateProfileApplicationStatus(userId, "waitlisted");

        var dto = MapToDTO(inserted.Models.First());

        return Response<AffiliateApplicationDTO>.SuccessResponse(
            dto, "Application submitted to waitlist. You'll be notified when a spot opens.");
    }

    private async Task UpdateProfileApplicationStatus(Guid userId, string status)
    {
        await _adminClient
            .From<Profiles>()
            .Where(p => p.Id == userId)
            .Set(p => p.AffiliateApplicationStatus!, status)
            .Update();
    }

    private async Task UpdateProfileAffiliateTier(Guid userId, string tier)
    {
        await _adminClient
            .From<Profiles>()
            .Where(p => p.Id == userId)
            .Set(p => p.AffiliateTier!, tier)
            .Update();
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private AffiliateApplicationDTO MapToDTO(AffiliateApplication app)
    {
        return new AffiliateApplicationDTO
        {
            Id = app.Id,
            UserId = app.UserId,
            FullName = app.FullName,  //  ADD
            Email = app.Email,  //  ADD
            SocialHandles = app.SocialHandles,
            AudienceSize = app.AudienceSize,
            ContentNiche = app.ContentNiche,
            PortfolioUrl = app.PortfolioUrl,
            Status = app.Status,
            WhyMuuqwear = app.WhyMuuqwear,  //  ADD
            SampleFiles = app.SampleFiles,  //  ADD
            SubmittedAt = app.SubmittedAt,
            ReviewedAt = app.ReviewedAt,
            AdminNotes = app.AdminNotes
        };
    }

    public async Task<Response<int>> GetSpotsRemaining()
    {
        try
        {
            var approvedCount = await GetApprovedAffiliateCount();
            var spotsRemaining = 500 - approvedCount;

            return Response<int>.SuccessResponse(
                spotsRemaining < 0 ? 0 : spotsRemaining,
                "Spots remaining calculated");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // APPROVE APPLICATION & GENERATE CODE
    // =============================================
    public async Task<Response<bool>> ApproveApplication(Guid applicationId)
    {
        try
        {
            // Step 1: Get the application
            var application = await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.Id == applicationId)
                .Single();

            if (application == null)
                return Response<bool>.Fail("Application not found");

            if (application.Status == "approved")
                return Response<bool>.Fail("Application already approved");

            // Step 2: Get user profile
            var profile = await _adminClient
                .From<Profiles>()
                .Where(p => p.Id == application.UserId)
                .Single();

            if (profile == null)
                return Response<bool>.Fail("User profile not found");

            // Step 3: Generate unique affiliate code
            string affiliateCode = await GenerateUniqueAffiliateCode(profile.FullName ?? "USER");

            // Step 4: Update profile with affiliate data
            await _adminClient
                .From<Profiles>()
                .Where(p => p.Id == application.UserId)
                .Set(p => p.AffiliateApplicationStatus!, "approved")
                .Set(p => p.AffiliateTier!, "bronze")
                .Set(p => p.AffiliateCode!, affiliateCode)
                .Set(p => p.AffiliateItemsSold!, 0)
                .Set(p => p.AffiliateCommissionEarned!, 0m)
                .Set(p => p.AffiliateBonusEarned!, 0m)
                .Update();

            // Step 5: Update application status
            await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.Id == applicationId)
                .Set(a => a.Status!, "approved")
                .Set(a => a.ReviewedAt!, DateTime.UtcNow)
                .Update();

            // TODO: Send welcome email with affiliate link (Phase 2)

            return Response<bool>.SuccessResponse(
                true,
                $"Application approved. Affiliate code: {affiliateCode}");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error approving application: {ex.Message}");
        }
    }

    private async Task<string> GenerateUniqueAffiliateCode(string fullName)
    {
        // Strategy: FirstName + 4 random digits
        // Example: "John Smith" → "JOHN2847"

        string firstName = fullName.Split(' ')[0].ToUpper();

        // Remove special characters and limit to 10 chars
        firstName = new string(firstName.Where(char.IsLetterOrDigit).ToArray());
        if (firstName.Length > 10)
            firstName = firstName.Substring(0, 10);

        if (string.IsNullOrEmpty(firstName))
            firstName = "USER"; // Fallback

        string code;
        int attempts = 0;

        do
        {
            string randomDigits = new Random().Next(1000, 9999).ToString();
            code = $"{firstName}{randomDigits}";
            attempts++;

            // Fallback: If name-based fails after 5 tries, use random 8-char code
            if (attempts > 5)
            {
                code = GenerateRandomCode(8);
            }

        } while (await CodeExists(code));

        return code;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task<bool> CodeExists(string code)
    {
        var result = await _adminClient
            .From<Profiles>()
            .Where(p => p.AffiliateCode == code)
            .Get();

        return result.Models.Any();
    }

    // =============================================
    // GET AFFILIATE INFO (Dashboard)
    // =============================================
    public async Task<Response<AffiliateInfoDTO>> GetAffiliateInfo(Guid userId)
    {
        try
        {
            // Get user profile
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile == null || string.IsNullOrEmpty(profile.AffiliateCode))
                return Response<AffiliateInfoDTO>.Fail("No affiliate account found");

            if (profile.AffiliateApplicationStatus != "approved")
                return Response<AffiliateInfoDTO>.Fail("Not an approved affiliate");

            if (string.IsNullOrEmpty(profile.AffiliateCode))
                return Response<AffiliateInfoDTO>.Fail("Affiliate code not generated");

            var clicks = await _client
                       .From<AffiliateClick>()
                       .Where(c => c.AffiliateCode == profile.AffiliateCode)
                       .Get();

            var referrals = await _client
            .From<AffiliateReferral>()
            .Where(r => r.AffiliateCode == profile.AffiliateCode)
            .Get();


            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var commissionThisMonth = referrals.Models
                .Where(r => r.CreatedAt >= startOfMonth)
                .Sum(r => r.CommissionAmount);

            //  ADD THIS - Calculate pending commission
            var commissionPending = referrals.Models
                .Where(r => r.Status == "pending")
                .Sum(r => r.CommissionAmount);

            // Build full affiliate link
            string affiliateLink = $"http://localhost:5276/?ref={profile.AffiliateCode}";

            var info = new AffiliateInfoDTO
            {
                AffiliateCode = profile.AffiliateCode,
                AffiliateLink = affiliateLink,
                Tier = profile.AffiliateTier ?? "bronze",
                ItemsSold = profile.AffiliateItemsSold,
                CommissionEarned = profile.AffiliateCommissionEarned,
                BonusEarned = profile.AffiliateBonusEarned,
                Conversions = referrals.Models.Count,
                TotalClicks = profile.AffiliateTotalClicks, // TODO: Implement in Task 1F
                CommissionThisMonth = commissionThisMonth,      //  ADD THIS
                CommissionPending = commissionPending
            };

            return Response<AffiliateInfoDTO>.SuccessResponse(info);
        }
        catch (Exception ex)
        {
            return Response<AffiliateInfoDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // VALIDATE AFFILIATE CODE (For Middleware)
    // =============================================
    public async Task<Response<bool>> ValidateAffiliateCode(string affiliateCode)
    {
        try
        {
            // Check if affiliate code exists and user is approved
            var result = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == affiliateCode &&
                           p.AffiliateApplicationStatus == "approved")
                .Get();

            bool isValid = result.Models.Any();

            return Response<bool>.SuccessResponse(
                isValid,
                isValid ? "Valid affiliate code" : "Invalid affiliate code");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error validating affiliate code: {ex.Message}");
        }
    }

    // =============================================
    // TRACK AFFILIATE CLICK (For Middleware)
    // =============================================

    // =============================================
    // TRACK AFFILIATE CLICK (For Middleware)
    // =============================================
    public async Task<Response<bool>> TrackClick(TrackClickRequestDTO request)
    {
        try
        {
            // Step 1: Insert click record into affiliate_clicks table
            var click = new AffiliateClick
            {
                Id = Guid.NewGuid(),
                AffiliateCode = request.AffiliateCode,
                ClickedAt = DateTime.UtcNow,
                IpAddress = request.IpAddress ?? "",
                UserAgent = request.UserAgent ?? "",
                ReferrerUrl = request.ReferrerUrl ?? "",
                Converted = false
            };

            await _client
                .From<AffiliateClick>()
                .Insert(click);

            // Step 2: Increment total clicks count directly (no RPC needed)
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == request.AffiliateCode)
                .Single();

            if (profile != null)
            {
                await _client
                    .From<Profiles>()
                    .Where(p => p.AffiliateCode == request.AffiliateCode)
                    .Set(p => p.AffiliateTotalClicks!, profile.AffiliateTotalClicks + 1)
                    .Update();
            }

            return Response<bool>.SuccessResponse(true, "Click tracked successfully");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error tracking click: {ex.Message}");
        }
    }
    // =============================================
    // CHECK FOR RECENT CLICK (Anti-Spam)
    // =============================================
    public async Task<Response<bool>> HasRecentClick(string affiliateCode, string ipAddress)
    {
        try
        {
            // Check if this IP clicked this affiliate link in last 24 hours
            var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

            // Chain Where conditions separately (not with &&)
            var result = await _client
                .From<AffiliateClick>()
                .Where(c => c.AffiliateCode == affiliateCode)
                .Where(c => c.IpAddress == ipAddress)
                .Where(c => c.ClickedAt >= twentyFourHoursAgo)
                .Get();

            bool hasRecentClick = result.Models.Any();

            return Response<bool>.SuccessResponse(
                hasRecentClick,
                hasRecentClick ? "Recent click found" : "No recent click");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error checking recent click: {ex.Message}");
        }
    }
    // =============================================
    // GET COMMISSION RATE FOR AFFILIATE
    // =============================================
    public async Task<Response<int>> GetCommissionRate(string affiliateCode)
    {
        try
        {
            // Get affiliate's profile to check their tier
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == affiliateCode)
                .Single();

            if (profile == null)
                return Response<int>.Fail("Affiliate not found");

            // Map tier to commission rate
            int rate = profile.AffiliateTier?.ToLower() switch
            {
                "bronze" => 5,
                "silver" => 10,
                "gold" => 15,
                _ => 5  // Default to bronze if tier is null or unknown
            };

            return Response<int>.SuccessResponse(
                rate,
                $"{profile.AffiliateTier ?? "Bronze"} tier: {rate}% commission");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail($"Error getting commission rate: {ex.Message}");
        }
    }
    // =============================================
    // TRACK ORDER REFERRAL
    // =============================================
    public async Task<Response<bool>> TrackOrderReferral(
     Guid orderId,
     Guid userId,
     decimal orderTotal,
     string? affiliateCode)
    {
        try
        {
            // STEP 1: Early exit
            if (string.IsNullOrEmpty(affiliateCode))
            {
                return Response<bool>.SuccessResponse(false, "No affiliate referral");
            }

            // STEP 2: Validate
            var validationResponse = await ValidateAffiliateCode(affiliateCode);
            if (!validationResponse.Success || !validationResponse.Data)
            {
                return Response<bool>.Fail($"Invalid affiliate code: {affiliateCode}");
            }

            // STEP 3: Get rate
            var rateResponse = await GetCommissionRate(affiliateCode);
            if (!rateResponse.Success)
            {
                return Response<bool>.Fail("Failed to get commission rate");
            }

            int rate = rateResponse.Data;

            // STEP 4: Calculate commission
            decimal commissionAmount = orderTotal * (rate / 100m);

            // STEP 5: Create referral record
            var referral = new AffiliateReferral
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                AffiliateCode = affiliateCode,
                UserId = userId,
                OrderTotal = orderTotal,
                CommissionAmount = commissionAmount,
                CommissionRate = rate,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _client.From<AffiliateReferral>().Insert(referral);

            //  STEP 6: Mark click as converted (SIMPLE VERSION)
            try
            {
                // Only update if there's an unconverted click
                var clicks = await _client
                    .From<AffiliateClick>()
                    .Where(c => c.AffiliateCode == affiliateCode)
                    .Where(c => c.Converted == false)
                    .Get();

                var recentClick = clicks?.Models?.FirstOrDefault();

                if (recentClick != null)
                {
                    recentClick.Converted = true;
                    await _client.From<AffiliateClick>().Update(recentClick);
                    Console.WriteLine($" [AffiliateService] First conversion - click marked");
                }
                else
                {
                    Console.WriteLine($"ℹ️ [AffiliateService] Additional conversion (click already marked)");
                }
            }
            catch (Exception clickEx)
            {
                Console.WriteLine($"⚠️ [AffiliateService] Click update: {clickEx.Message}");
            }

            // STEP 7: Update profile stats
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == affiliateCode)
                .Single();

            if (profile != null)
            {
                await _client
                    .From<Profiles>()
                    .Where(p => p.AffiliateCode == affiliateCode)
                    .Set(p => p.AffiliateItemsSold!, profile.AffiliateItemsSold + 1)
                    .Set(p => p.AffiliateCommissionEarned!, profile.AffiliateCommissionEarned + commissionAmount)
                    .Update();
            }

            return Response<bool>.SuccessResponse(true, $"Tracked: ${commissionAmount:F2}");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // GET PERFORMANCE CHART DATA (LAST 30 DAYS)
    // =============================================
    // =============================================
    // GET PERFORMANCE CHART DATA (LAST 30 DAYS)
    // =============================================
    // =============================================
    // GET PERFORMANCE CHART DATA (LAST 30 DAYS)
    // =============================================
    public async Task<Response<PerformanceChartDTO>> GetPerformanceChart(string affiliateCode)
    {
        try
        {
            // Step 1: Calculate date range (last 30 days)
            var endDate = DateTime.UtcNow.Date; // Today at midnight
            var startDate = endDate.AddDays(-29); // 30 days ago (including today = 30 days)

            Console.WriteLine($"📊 [Chart] Loading data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Step 2: Get all clicks in the last 30 days
            var clicks = await _client
                .From<AffiliateClick>()
                .Where(c => c.AffiliateCode == affiliateCode)
                .Where(c => c.ClickedAt >= startDate)
                .Get();

            Console.WriteLine($"📊 [Chart] Found {clicks.Models.Count} clicks");

            // Step 3: Get all conversions in the last 30 days
            var referrals = await _client
                .From<AffiliateReferral>()
                .Where(r => r.AffiliateCode == affiliateCode)
                .Where(r => r.CreatedAt >= startDate)
                .Get();

            Console.WriteLine($"📊 [Chart] Found {referrals.Models.Count} conversions");

            // Step 4: Group clicks by date
            var clicksByDate = clicks.Models
                .GroupBy(c => c.ClickedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // Step 5: Group conversions by date
            var conversionsByDate = referrals.Models
                .GroupBy(r => r.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // Step 6: Create array of 30 days with data
            var dailyStats = new List<DailyPerformanceDTO>();

            for (int i = 0; i < 30; i++)
            {
                var date = startDate.AddDays(i);

                // Get clicks for this day (0 if none)
                clicksByDate.TryGetValue(date, out int clickCount);

                // Get conversions for this day (0 if none)
                conversionsByDate.TryGetValue(date, out int conversionCount);

                dailyStats.Add(new DailyPerformanceDTO
                {
                    Day = i + 1,              // 1-30
                    Date = date,              // Actual date
                    Clicks = clickCount,      // Clicks that day (0 if none)
                    Conversions = conversionCount  // Conversions that day (0 if none)
                });
            }

            var chartData = new PerformanceChartDTO
            {
                DailyStats = dailyStats
            };

            Console.WriteLine($" [Chart] Generated {dailyStats.Count} days of data");

            return Response<PerformanceChartDTO>.SuccessResponse(
                chartData,
                "Chart data loaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Chart] Error: {ex.Message}");
            return Response<PerformanceChartDTO>.Fail($"Error loading chart data: {ex.Message}");
        }
    }



    // =============================================
    // PARTNER STORE: GET PRODUCTS WITH PAGINATION
    // =============================================
    public async Task<Response<PaginatedResponse<PartnerStoreProductDTO>>> GetPartnerStoreProducts(
        Guid userId,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            Console.WriteLine($" [PartnerStore] Fetching products - Page {page}, Size {pageSize}");

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 15;

            // Step 1: Verify user is approved affiliate
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile?.AffiliateApplicationStatus != "approved")
            {
                return Response<PaginatedResponse<PartnerStoreProductDTO>>.Fail(
                    "Only approved affiliates can access partner store");
            }

            // Step 2: Get total count of active products
            var allProductsResponse = await _client
                .From<Product>()
                .Where(p => p.IsActive == true)
                .Get();

            var totalCount = allProductsResponse.Models?.Count ?? 0;

            if (totalCount == 0)
            {
                Console.WriteLine(" [PartnerStore] No products found");
                return Response<PaginatedResponse<PartnerStoreProductDTO>>.SuccessResponse(
                    new PaginatedResponse<PartnerStoreProductDTO>
                    {
                        Data = new List<PartnerStoreProductDTO>(),
                        TotalCount = 0,
                        Page = 1,
                        PageSize = pageSize,
                        TotalPages = 0,
                        HasMore = false,
                    },
                    "No products available");
            }

            // Step 3: Calculate pagination
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var skip = (page - 1) * pageSize;

            // Step 4: Get paginated products
            var pagedProducts = allProductsResponse.Models!
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            // Step 5: Get product IDs for stock lookup
            var productIds = pagedProducts.Select(p => p.Id).ToList();

            // Step 6: Fetch stock for paginated products only
            var stockResponse = await _client
                .From<ProductSizeStock>()
                .Get();

            var relevantStock = stockResponse.Models?
                .Where(s => productIds.Contains(s.ProductId))
                .ToList() ?? new List<ProductSizeStock>();

            Console.WriteLine($" [PartnerStore] Found {relevantStock.Count} stock records");

            var stockByProduct = relevantStock
                .GroupBy(s => s.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Step 7: Transform to DTOs
            var productDTOs = pagedProducts
                .Select(p => MapToPartnerStoreProduct(p, stockByProduct))
                .ToList();

            // Step 8: Build paginated response using your existing model
            var paginatedResponse = new PaginatedResponse<PartnerStoreProductDTO>
            {
                Data = productDTOs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
            };

            Console.WriteLine($"[PartnerStore] Returned page {page}/{totalPages} ({productDTOs.Count} products)");

            return Response<PaginatedResponse<PartnerStoreProductDTO>>.SuccessResponse(
                paginatedResponse,
                "Products loaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Error fetching products: {ex.Message}");
            return Response<PaginatedResponse<PartnerStoreProductDTO>>.Fail(
                $"Failed to load products: {ex.Message}");
        }
    }

    // =============================================
    // HELPER: MAP PRODUCT TO DTO WITH STOCK
    // =============================================
    private PartnerStoreProductDTO MapToPartnerStoreProduct(
        Product product,
        Dictionary<Guid, List<ProductSizeStock>> stockByProduct)
    {
        // Get stock for this product
        var productStock = stockByProduct.ContainsKey(product.Id)
            ? stockByProduct[product.Id]
            : new List<ProductSizeStock>();

        // Map to size stock DTOs
        var sizeStockDTOs = productStock
            .Select(s => new ProductSizeStockDTO
            {
                Size = s.Size,
                Quantity = s.Quantity
            })
            .OrderBy(s => GetSizeOrder(s.Size)) // S, M, L, XL order
            .ToList();

        // Calculate totals
        var totalStock = productStock.Sum(s => s.Quantity);
        var inStock = totalStock > 0;

        // Calculate discount prices
        var originalPrice = product.Price;
        var discountedPrice = CalculateDiscountedPrice(originalPrice);
        var discountAmount = CalculateDiscountAmount(originalPrice);

        return new PartnerStoreProductDTO
        {
            Id = product.Id,
            Name = product.Name!,
            Description = product.Description ?? string.Empty,
            Category = product.Category ?? "General",
            ImageUrl = product.ImageUrl ?? string.Empty,
            OriginalPrice = originalPrice,
            DiscountedPrice = discountedPrice,
            DiscountAmount = discountAmount,
            DiscountPercentage = 25,
            InStock = inStock,
            TotalStock = totalStock,
            SizeStock = sizeStockDTOs
        };
    }

    // =============================================
    // HELPER: GET SIZE SORT ORDER
    // =============================================
    private int GetSizeOrder(string size)
    {
        return size.ToUpper() switch
        {
            "XS" => 1,
            "S" => 2,
            "M" => 3,
            "L" => 4,
            "XL" => 5,
            "XXL" => 6,
            _ => 99 // Unknown sizes at end
        };
    }
    // =============================================
    // PARTNER STORE: GET PURCHASE LIMIT STATUS
    // =============================================
    public async Task<Response<AffiliatePurchaseLimitDTO>> GetPurchaseLimitStatus(Guid userId)
    {
        try
        {
            Console.WriteLine($"[PartnerStore] Getting limit status for user: {userId}");

            // Step 1: Verify affiliate
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile?.AffiliateApplicationStatus != "approved")
            {
                return Response<AffiliatePurchaseLimitDTO>.Fail(
                    "User is not an approved affiliate");
            }

            // Step 2: Get items purchased this month using database function
            var itemsPurchased = await GetItemsPurchasedThisMonth(userId);

            // Step 3: Calculate dates
            var monthStart = GetCurrentMonthStart();
            var nextReset = GetNextMonthStart();

            // Step 4: Build DTO
            var limitDTO = new AffiliatePurchaseLimitDTO
            {
                ItemsPurchasedThisMonth = itemsPurchased,
                ItemsRemaining = Math.Max(0, MONTHLY_PURCHASE_LIMIT - itemsPurchased),
                MonthlyLimit = MONTHLY_PURCHASE_LIMIT,
                MonthStartDate = monthStart,
                NextResetDate = nextReset,
                LimitReached = itemsPurchased >= MONTHLY_PURCHASE_LIMIT
            };

            Console.WriteLine($" [PartnerStore] Limit: {itemsPurchased}/{MONTHLY_PURCHASE_LIMIT}");

            return Response<AffiliatePurchaseLimitDTO>.SuccessResponse(
                limitDTO,
                "Limit status retrieved");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [PartnerStore] Error getting limit: {ex.Message}");
            return Response<AffiliatePurchaseLimitDTO>.Fail(
                $"Failed to get limit status: {ex.Message}");
        }
    }

    // =============================================
    // HELPER: GET ITEMS PURCHASED THIS MONTH
    // =============================================
    // =============================================
    // HELPER: GET ITEMS PURCHASED THIS MONTH
    // =============================================
    private async Task<int> GetItemsPurchasedThisMonth(Guid userId)
    {
        try
        {
            var monthStart = GetCurrentMonthStart();
            var monthEnd = GetNextMonthStart();

            Console.WriteLine($"📊 [PartnerStore] Counting purchases from {monthStart:yyyy-MM-dd} to {monthEnd:yyyy-MM-dd}");

            // Query affiliate_personal_purchases for current month
            var purchasesResponse = await _client
                .From<AffiliatePersonalPurchase>()
                .Where(p => p.UserId == userId)
                .Where(p => p.PurchasedAt >= monthStart)
                .Where(p => p.PurchasedAt < monthEnd)
                .Where(p => p.Status == "completed") // Only count completed purchases
                .Get();

            if (purchasesResponse.Models == null || !purchasesResponse.Models.Any())
            {
                Console.WriteLine($"[PartnerStore] No purchases found this month");
                return 0;
            }

            // Sum quantities
            var totalItems = purchasesResponse.Models.Sum(p => p.Quantity);

            Console.WriteLine($" [PartnerStore] Found {totalItems} items purchased this month");

            return totalItems;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PartnerStore] Error querying purchases: {ex.Message}");
            return 0; // Safe default
        }
    }

    // =============================================
    // HELPER: GET CURRENT MONTH START
    // =============================================
    private DateTime GetCurrentMonthStart()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    // =============================================
    // HELPER: GET NEXT MONTH START
    // =============================================
    private DateTime GetNextMonthStart()
    {
        var currentStart = GetCurrentMonthStart();
        return currentStart.AddMonths(1);
    }


    // =============================================
    // PARTNER STORE: VALIDATE PURCHASE
    // =============================================
    public async Task<Response<bool>> CanPurchase(Guid userId, int quantity)
    {
        try
        {
            Console.WriteLine($"✔️ [PartnerStore] Checking if user {userId} can buy {quantity} items");

            // Step 1: Validate quantity
            if (quantity <= 0)
            {
                return Response<bool>.Fail("Quantity must be greater than 0");
            }

            // Step 2: Get current purchases
            var itemsPurchased = await GetItemsPurchasedThisMonth(userId);

            // Step 3: Calculate if within limit
            var totalAfterPurchase = itemsPurchased + quantity;
            var canPurchase = totalAfterPurchase <= MONTHLY_PURCHASE_LIMIT;

            if (!canPurchase)
            {
                var remaining = MONTHLY_PURCHASE_LIMIT - itemsPurchased;
                return Response<bool>.Fail(
                    $"Purchase would exceed monthly limit. You have {remaining} items remaining.");
            }

            Console.WriteLine($" [PartnerStore] Purchase allowed: {totalAfterPurchase}/{MONTHLY_PURCHASE_LIMIT}");

            return Response<bool>.SuccessResponse(true, "Purchase allowed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [PartnerStore] Error checking purchase: {ex.Message}");
            return Response<bool>.Fail($"Error validating purchase: {ex.Message}");
        }
    }

    // =============================================
    // HELPER: CALCULATE DISCOUNTED PRICE
    // =============================================
    private decimal CalculateDiscountedPrice(decimal originalPrice)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(originalPrice));
        }

        var discountedPrice = originalPrice * (1 - AFFILIATE_DISCOUNT_PERCENTAGE);
        return Math.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
    }

    // =============================================
    // HELPER: CALCULATE DISCOUNT AMOUNT
    // =============================================
    private decimal CalculateDiscountAmount(decimal originalPrice)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(originalPrice));
        }

        var discountAmount = originalPrice * AFFILIATE_DISCOUNT_PERCENTAGE;
        return Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
    }

    // =============================================
    // GET RECENT REFERRALS
    // =============================================
    /// <summary>
    /// Get recent referrals for an affiliate (last 10)
    /// </summary>
    public async Task<Response<List<RecentReferralDTO>>> GetRecentReferrals(Guid userId)
    {
        try
        {
            Console.WriteLine($" [Affiliate] Fetching recent referrals for user: {userId}");

            // Step 1: Get affiliate's code
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile == null || string.IsNullOrEmpty(profile.AffiliateCode))
            {
                return Response<List<RecentReferralDTO>>.Fail("Affiliate profile not found");
            }

            var affiliateCode = profile.AffiliateCode;

            // Step 2: Fetch recent referrals (last 10)
            var referralsResponse = await _client
                .From<AffiliateReferral>()
                .Where(r => r.AffiliateCode == affiliateCode)
                .Order(r => r.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(10)
                .Get();

            var referrals = referralsResponse.Models ?? new List<AffiliateReferral>();

            if (!referrals.Any())
            {
                Console.WriteLine($" [Affiliate] No referrals found");
                return Response<List<RecentReferralDTO>>.SuccessResponse(
                    new List<RecentReferralDTO>(),
                    "No referrals yet");
            }

            // Step 3: Get order numbers for masking
            var orderIds = referrals.Select(r => r.OrderId).ToList();
            var ordersResponse = await _client
                .From<Order>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In,
                    orderIds.Select(id => id.ToString()).ToList())
                .Get();

            var orders = ordersResponse.Models ?? new List<Order>();
            var orderDict = orders.ToDictionary(o => o.Id, o => o.OrderNumber);

            // Step 4: Transform to DTOs with masked customer
            var recentReferrals = referrals.Select(r =>
            {
                var maskedCustomer = "****";  // Default

                if (orderDict.TryGetValue(r.OrderId, out var orderNumber))
                {
                    // Mask order number: MQ-ABC12345 → ***2345
                    maskedCustomer = MaskOrderNumber(orderNumber);
                }

                return new RecentReferralDTO
                {
                    CreatedAt = r.CreatedAt,
                    MaskedCustomer = maskedCustomer,
                    OrderTotal = r.OrderTotal,
                    CommissionAmount = r.CommissionAmount,
                    Status = r.Status
                };
            }).ToList();

            Console.WriteLine($"[Affiliate] Found {recentReferrals.Count} recent referrals");

            return Response<List<RecentReferralDTO>>.SuccessResponse(
                recentReferrals,
                "Recent referrals loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Affiliate] Error fetching recent referrals: {ex.Message}");
            return Response<List<RecentReferralDTO>>.Fail(
                $"Failed to load recent referrals: {ex.Message}");
        }
    }

    /// <summary>
    /// Mask order number for privacy (MQ-ABC12345 → ***2345)
    /// </summary>
    private string MaskOrderNumber(string orderNumber)
    {
        if (string.IsNullOrEmpty(orderNumber) || orderNumber.Length < 4)
            return "****";

        // Get last 4 characters
        var last4 = orderNumber.Substring(orderNumber.Length - 4);
        return $"***{last4}";
    }

}
