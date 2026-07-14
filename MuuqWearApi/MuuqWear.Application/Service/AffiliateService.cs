using Microsoft.Extensions.Caching.Memory;
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
    private const string TierCacheKey = "affiliate_tiers_all";
    private static readonly TimeSpan TierCacheTtl = TimeSpan.FromMinutes(5);
    private const decimal FallbackCommissionRate = 5m;

    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;
    private readonly IMemoryCache _cache;
    private const int MONTHLY_PURCHASE_LIMIT = 20;
    private const decimal AFFILIATE_DISCOUNT_PERCENTAGE = 0.25m;

    public AffiliateService(
        SupabaseClientFactory factory,
        SupabaseAdminClientFactory adminFactory,
        IMemoryCache cache)
    {
        _client = factory.CreateClient();
        _adminClient = adminFactory.CreateClient();
        _cache = cache;
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

            var enrichment = await BuildApplicationEnrichmentAsync(
                applications.Select(a => a.UserId));

            var dtos = applications
                .Select(app => MapToDTO(app, enrichment.GetValueOrDefault(app.UserId)))
                .ToList();

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

            if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                var capacityError = await EnsureTierHasCapacityAsync("bronze");
                if (capacityError != null)
                    return Response<AffiliateApplicationDTO>.Fail(capacityError);
            }

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

            // If approved, set initial tier and join metadata
            if (status == "approved")
            {
                await _adminClient
                    .From<Profiles>()
                    .Where(p => p.Id == application.UserId)
                    .Set(p => p.AffiliateTier!, "bronze")
                    .Set(p => p.AffiliateIsActive!, true)
                    .Set(p => p.AffiliateApprovedAt!, DateTime.UtcNow)
                    .Update();
            }

            var enrichment = await BuildApplicationEnrichmentAsync(new[] { updatedApp.UserId });
            var dto = MapToDTO(updatedApp, enrichment.GetValueOrDefault(updatedApp.UserId));

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

    private AffiliateApplicationDTO MapToDTO(
        AffiliateApplication app,
        ApplicationEnrichment? enrichment = null)
    {
        var tier = enrichment?.AffiliateTier ?? "none";
        var joinDate = enrichment?.JoinDate
            ?? app.ReviewedAt
            ?? app.SubmittedAt;

        return new AffiliateApplicationDTO
        {
            Id = app.Id,
            UserId = app.UserId,
            FullName = app.FullName,
            Email = app.Email,
            SocialHandles = app.SocialHandles,
            AudienceSize = app.AudienceSize,
            ContentNiche = app.ContentNiche,
            PortfolioUrl = app.PortfolioUrl,
            Status = app.Status,
            WhyMuuqwear = app.WhyMuuqwear,
            SampleFiles = app.SampleFiles,
            SubmittedAt = app.SubmittedAt,
            ReviewedAt = app.ReviewedAt,
            AdminNotes = app.AdminNotes,
            AffiliateTier = tier,
            ItemsSold = enrichment?.ItemsSold ?? 0,
            CommissionEarned = enrichment?.CommissionEarned ?? 0m,
            CommissionRatePercent = enrichment?.CommissionRatePercent ?? 0m,
            LastSaleAt = enrichment?.LastSaleAt,
            JoinDate = joinDate,
            IsActive = enrichment?.IsActive ?? true
        };
    }

    private sealed class ApplicationEnrichment
    {
        public string AffiliateTier { get; set; } = "none";
        public int ItemsSold { get; set; }
        public decimal CommissionEarned { get; set; }
        public decimal CommissionRatePercent { get; set; }
        public DateTime? LastSaleAt { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool IsActive { get; set; } = true;
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

            var capacityError = await EnsureTierHasCapacityAsync("bronze");
            if (capacityError != null)
                return Response<bool>.Fail(capacityError);

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
                .Set(p => p.AffiliateIsActive!, true)
                .Set(p => p.AffiliateApprovedAt!, DateTime.UtcNow)
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
            // Approved + active affiliates earn commissions / remain valid for checkout
            var result = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == affiliateCode &&
                           p.AffiliateApplicationStatus == "approved" &&
                           p.AffiliateIsActive == true)
                .Get();

            bool isValid = result.Models.Any();

            return Response<bool>.SuccessResponse(
                isValid,
                isValid ? "Valid affiliate code" : "Invalid or inactive affiliate code");
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
    public async Task<Response<decimal>> GetCommissionRate(string affiliateCode)
    {
        try
        {
            var profile = await _client
                .From<Profiles>()
                .Where(p => p.AffiliateCode == affiliateCode)
                .Single();

            if (profile == null)
                return Response<decimal>.Fail("Affiliate not found");

            var slug = string.IsNullOrWhiteSpace(profile.AffiliateTier)
                ? "bronze"
                : profile.AffiliateTier.Trim().ToLowerInvariant();

            var tier = await GetTierBySlugCachedAsync(slug);
            if (tier == null || !tier.IsActive)
                tier = await GetTierBySlugCachedAsync("bronze");

            var rate = tier?.CommissionRatePercent ?? FallbackCommissionRate;
            var display = tier?.DisplayName ?? profile.AffiliateTier ?? "Bronze";

            return Response<decimal>.SuccessResponse(
                rate,
                $"{display} tier: {rate}% commission");
        }
        catch (Exception ex)
        {
            return Response<decimal>.Fail($"Error getting commission rate: {ex.Message}");
        }
    }

    public async Task<Response<List<AffiliateTierDTO>>> GetAdminTiers()
    {
        try
        {
            var tiers = await LoadAllTiersFromDbAsync();
            var counts = await CountActiveAffiliatesByTierAsync();
            return Response<List<AffiliateTierDTO>>.SuccessResponse(
                tiers.Select(t => MapTierToDto(t, counts.GetValueOrDefault(t.Slug))).ToList(),
                "Tiers fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliateTierDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AffiliateTierDTO>> GetAdminTierBySlug(string slug)
    {
        try
        {
            var normalized = NormalizeSlug(slug);
            if (normalized == null)
                return Response<AffiliateTierDTO>.Fail("Invalid tier slug");

            var tier = await FetchTierBySlugAsync(normalized);
            if (tier == null)
                return Response<AffiliateTierDTO>.Fail("Tier not found");

            var counts = await CountActiveAffiliatesByTierAsync();
            return Response<AffiliateTierDTO>.SuccessResponse(
                MapTierToDto(tier, counts.GetValueOrDefault(tier.Slug)), "Tier fetched");
        }
        catch (Exception ex)
        {
            return Response<AffiliateTierDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AffiliateTierDTO>> UpdateAdminTier(
        string slug, UpdateAffiliateTierDTO request, Guid adminUserId)
    {
        try
        {
            var normalized = NormalizeSlug(slug);
            if (normalized == null)
                return Response<AffiliateTierDTO>.Fail("Invalid tier slug");

            var existing = await FetchTierBySlugAsync(normalized);
            if (existing == null)
                return Response<AffiliateTierDTO>.Fail("Tier not found");

            var proposed = CloneTier(existing);
            ApplyTierUpdate(proposed, request);

            var validationError = await ValidateTierUpdateAsync(proposed);
            if (validationError != null)
                return Response<AffiliateTierDTO>.Fail(validationError);

            var updated = (await _adminClient
                .From<AffiliateTier>()
                .Filter("slug", Supabase.Postgrest.Constants.Operator.Equals, normalized)
                .Set(x => x.DisplayName, proposed.DisplayName)
                .Set(x => x.ItemsSoldThreshold, proposed.ItemsSoldThreshold)
                .Set(x => x.CommissionRatePercent, proposed.CommissionRatePercent)
                .Set(x => x.ReferralDiscountPercent, proposed.ReferralDiscountPercent)
                .Set(x => x.QuarterlyBonusPercent, proposed.QuarterlyBonusPercent)
                .Set(x => x.MaxAffiliates!, proposed.MaxAffiliates)
                .Set(x => x.Perks, proposed.Perks ?? new List<string>())
                .Set(x => x.SortOrder, proposed.SortOrder)
                .Set(x => x.IsActive, proposed.IsActive)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedBy!, adminUserId == Guid.Empty ? null : adminUserId)
                .Update()).Models.FirstOrDefault();

            if (updated == null)
                return Response<AffiliateTierDTO>.Fail("Failed to update tier");

            InvalidateTierCache();

            var counts = await CountActiveAffiliatesByTierAsync();
            return Response<AffiliateTierDTO>.SuccessResponse(
                MapTierToDto(updated, counts.GetValueOrDefault(updated.Slug)), "Tier updated");
        }
        catch (Exception ex)
        {
            return Response<AffiliateTierDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<List<AffiliateTierDTO>>> GetPublicTiers()
    {
        try
        {
            var tiers = await GetCachedTiersAsync();
            var active = tiers
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .Select(MapTierToDto)
                .ToList();

            return Response<List<AffiliateTierDTO>>.SuccessResponse(
                active, "Tiers fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliateTierDTO>>.Fail("Error: " + ex.Message);
        }
    }

    private async Task<List<AffiliateTier>> GetCachedTiersAsync()
    {
        if (_cache.TryGetValue(TierCacheKey, out List<AffiliateTier>? cached)
            && cached != null)
        {
            return cached;
        }

        var tiers = await LoadAllTiersFromDbAsync();
        _cache.Set(TierCacheKey, tiers, TierCacheTtl);
        return tiers;
    }

    private async Task<List<AffiliateTier>> LoadAllTiersFromDbAsync()
    {
        var result = await _adminClient
            .From<AffiliateTier>()
            .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return result.Models;
    }

    private async Task<AffiliateTier?> GetTierBySlugCachedAsync(string slug)
    {
        var tiers = await GetCachedTiersAsync();
        return tiers.FirstOrDefault(t =>
            t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<AffiliateTier?> FetchTierBySlugAsync(string slug)
    {
        var result = await _adminClient
            .From<AffiliateTier>()
            .Filter("slug", Supabase.Postgrest.Constants.Operator.Equals, slug)
            .Limit(1)
            .Get();

        return result.Models.FirstOrDefault();
    }

    private void InvalidateTierCache() => _cache.Remove(TierCacheKey);

    private async Task<string?> ValidateTierUpdateAsync(AffiliateTier proposed)
    {
        if (string.IsNullOrWhiteSpace(proposed.DisplayName))
            return "Display name is required";

        if (proposed.CommissionRatePercent < 0 || proposed.CommissionRatePercent > 100)
            return "Commission rate must be between 0 and 100";

        if (proposed.ReferralDiscountPercent < 0 || proposed.ReferralDiscountPercent > 100)
            return "Referral discount must be between 0 and 100";

        if (proposed.QuarterlyBonusPercent < 0 || proposed.QuarterlyBonusPercent > 100)
            return "Quarterly bonus must be between 0 and 100";

        if (proposed.MaxAffiliates.HasValue && proposed.MaxAffiliates.Value <= 0)
            return "Max affiliates must be null (unlimited) or a positive integer";

        if (proposed.ItemsSoldThreshold < 0)
            return "Items sold threshold cannot be negative";

        var allTiers = await LoadAllTiersFromDbAsync();
        var projected = allTiers
            .Select(t => t.Id == proposed.Id ? proposed : t)
            .ToList();

        if (!projected.Any(t => t.IsActive))
            return "At least one tier must remain active";

        if (proposed.Slug.Equals("bronze", StringComparison.OrdinalIgnoreCase)
            && !proposed.IsActive)
        {
            return "Cannot deactivate the bronze tier (default approval tier)";
        }

        var activeOrdered = projected
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Slug)
            .ToList();

        for (var i = 1; i < activeOrdered.Count; i++)
        {
            if (activeOrdered[i].ItemsSoldThreshold
                <= activeOrdered[i - 1].ItemsSoldThreshold)
            {
                return "Items sold thresholds must be strictly increasing by sort order "
                    + $"({activeOrdered[i - 1].DisplayName} < {activeOrdered[i].DisplayName})";
            }
        }

        return null;
    }

    private static void ApplyTierUpdate(AffiliateTier target, UpdateAffiliateTierDTO request)
    {
        if (request.DisplayName != null)
            target.DisplayName = request.DisplayName.Trim();

        if (request.ItemsSoldThreshold.HasValue)
            target.ItemsSoldThreshold = request.ItemsSoldThreshold.Value;

        if (request.CommissionRatePercent.HasValue)
            target.CommissionRatePercent = request.CommissionRatePercent.Value;

        if (request.ReferralDiscountPercent.HasValue)
            target.ReferralDiscountPercent = request.ReferralDiscountPercent.Value;

        if (request.QuarterlyBonusPercent.HasValue)
            target.QuarterlyBonusPercent = request.QuarterlyBonusPercent.Value;

        if (request.ClearMaxAffiliates)
            target.MaxAffiliates = null;
        else if (request.MaxAffiliates.HasValue)
            target.MaxAffiliates = request.MaxAffiliates.Value;

        if (request.Perks != null)
            target.Perks = request.Perks
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();

        if (request.SortOrder.HasValue)
            target.SortOrder = request.SortOrder.Value;

        if (request.IsActive.HasValue)
            target.IsActive = request.IsActive.Value;
    }

    private static AffiliateTier CloneTier(AffiliateTier source) => new()
    {
        Id = source.Id,
        Slug = source.Slug,
        DisplayName = source.DisplayName,
        ItemsSoldThreshold = source.ItemsSoldThreshold,
        CommissionRatePercent = source.CommissionRatePercent,
        ReferralDiscountPercent = source.ReferralDiscountPercent,
        QuarterlyBonusPercent = source.QuarterlyBonusPercent,
        MaxAffiliates = source.MaxAffiliates,
        Perks = source.Perks?.ToList() ?? new List<string>(),
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        UpdatedAt = source.UpdatedAt,
        UpdatedBy = source.UpdatedBy
    };

    private static string? NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return slug.Trim().ToLowerInvariant();
    }

    private static AffiliateTierDTO MapTierToDto(AffiliateTier tier, int currentAffiliateCount = 0) => new()
    {
        Id = tier.Id,
        Slug = tier.Slug,
        DisplayName = tier.DisplayName,
        ItemsSoldThreshold = tier.ItemsSoldThreshold,
        CommissionRatePercent = tier.CommissionRatePercent,
        ReferralDiscountPercent = tier.ReferralDiscountPercent,
        QuarterlyBonusPercent = tier.QuarterlyBonusPercent,
        MaxAffiliates = tier.MaxAffiliates,
        Perks = tier.Perks?.ToList() ?? new List<string>(),
        CurrentAffiliateCount = currentAffiliateCount,
        SortOrder = tier.SortOrder,
        IsActive = tier.IsActive,
        UpdatedAt = tier.UpdatedAt
    };

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

            decimal rate = rateResponse.Data;

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
                CommissionRate = (int)Math.Round(rate, MidpointRounding.AwayFromZero),
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
                    Console.WriteLine($"[AffiliateService] Additional conversion (click already marked)");
                }
            }
            catch (Exception clickEx)
            {
                Console.WriteLine($"[AffiliateService] Click update: {clickEx.Message}");
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


            // Step 2: Get all clicks in the last 30 days
            var clicks = await _client
                .From<AffiliateClick>()
                .Where(c => c.AffiliateCode == affiliateCode)
                .Where(c => c.ClickedAt >= startDate)
                .Get();


            // Step 3: Get all conversions in the last 30 days
            var referrals = await _client
                .From<AffiliateReferral>()
                .Where(r => r.AffiliateCode == affiliateCode)
                .Where(r => r.CreatedAt >= startDate)
                .Get();


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


            return Response<bool>.SuccessResponse(true, "Purchase allowed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PartnerStore] Error checking purchase: {ex.Message}");
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

            return Response<List<RecentReferralDTO>>.SuccessResponse(
                recentReferrals,
                "Recent referrals loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Affiliate] Error fetching recent referrals: {ex.Message}");
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

    // =============================================
    // ADMIN PAYOUTS
    // =============================================

    public async Task<Response<List<AffiliatePendingPayoutDTO>>> GetAdminPendingPayouts()
    {
        try
        {
            var pending = await _adminClient
                .From<AffiliateReferral>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "pending")
                .Get();

            var referrals = pending.Models;
            if (referrals.Count == 0)
            {
                return Response<List<AffiliatePendingPayoutDTO>>.SuccessResponse(
                    new List<AffiliatePendingPayoutDTO>(), "Pending payouts fetched");
            }

            var codes = referrals
                .Select(r => r.AffiliateCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var profiles = await FetchAffiliateProfilesByCodesAsync(codes);
            var approvedProfiles = profiles
                .Where(p => string.Equals(
                    p.AffiliateApplicationStatus, "approved", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(p.AffiliateCode))
                .GroupBy(p => p.AffiliateCode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            var groups = referrals
                .Where(r => approvedProfiles.ContainsKey(r.AffiliateCode))
                .GroupBy(r => r.AffiliateCode, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var profile = approvedProfiles[g.Key];
                    return new AffiliatePendingPayoutDTO
                    {
                        AffiliateCode = g.Key,
                        AffiliateName = profile.FullName ?? g.Key,
                        AffiliateTier = profile.AffiliateTier ?? "bronze",
                        TotalAmount = g.Sum(r => r.CommissionAmount),
                        ReferralCount = g.Count(),
                        OldestPendingDate = g.Min(r => r.CreatedAt),
                        Status = "pending",
                        PaymentMethod = NormalizePaymentMethod(
                            profile.AffiliatePreferredPaymentMethod)
                    };
                })
                .Where(p => p.TotalAmount > 0)
                .OrderBy(p => p.OldestPendingDate)
                .ToList();

            return Response<List<AffiliatePendingPayoutDTO>>.SuccessResponse(
                groups, "Pending payouts fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliatePendingPayoutDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<List<AffiliatePendingReferralDTO>>> GetAdminPendingReferrals(
        string affiliateCode)
    {
        try
        {
            var code = affiliateCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                return Response<List<AffiliatePendingReferralDTO>>.Fail("Affiliate code is required");

            var profile = await GetApprovedAffiliateByCodeAsync(code);
            if (profile == null)
                return Response<List<AffiliatePendingReferralDTO>>.Fail("Affiliate not found");

            var pending = await _adminClient
                .From<AffiliateReferral>()
                .Filter("affiliate_code", Supabase.Postgrest.Constants.Operator.Equals, code)
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "pending")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var referrals = pending.Models;
            var orderIds = referrals.Select(r => r.OrderId).Distinct().ToList();
            var orderNumbers = await FetchOrderNumbersAsync(orderIds);

            var items = referrals.Select(r =>
            {
                orderNumbers.TryGetValue(r.OrderId, out var orderNumber);
                return new AffiliatePendingReferralDTO
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    OrderNumber = orderNumber ?? string.Empty,
                    OrderTotal = r.OrderTotal,
                    CommissionAmount = r.CommissionAmount,
                    CommissionRate = r.CommissionRate,
                    CreatedAt = r.CreatedAt,
                    MaskedCustomer = MaskOrderNumber(orderNumber ?? string.Empty)
                };
            }).ToList();

            return Response<List<AffiliatePendingReferralDTO>>.SuccessResponse(
                items, "Pending referrals fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliatePendingReferralDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AffiliatePayoutResultDTO>> ProcessAdminPayout(
        string affiliateCode, ProcessAffiliatePayoutDTO request, Guid adminUserId)
    {
        try
        {
            if (adminUserId == Guid.Empty)
                return Response<AffiliatePayoutResultDTO>.Fail("Not authenticated");

            var code = affiliateCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                return Response<AffiliatePayoutResultDTO>.Fail("Affiliate code is required");

            var paymentMethod = string.IsNullOrWhiteSpace(request?.PaymentMethod)
                ? "manual"
                : request!.PaymentMethod.Trim().ToLowerInvariant();

            var allowedMethods = new[] { "manual", "paypal", "bank_transfer", "store_credit" };
            if (!allowedMethods.Contains(paymentMethod))
            {
                return Response<AffiliatePayoutResultDTO>.Fail(
                    "Invalid payment method. Allowed: manual, paypal, bank_transfer, store_credit");
            }

            var adminNotes = request?.AdminNotes?.Trim();
            if (adminNotes != null && adminNotes.Length > 2000)
                return Response<AffiliatePayoutResultDTO>.Fail("Admin notes max length is 2000 characters");

            var rpcParams = new Dictionary<string, object>
            {
                { "p_affiliate_code", code },
                { "p_processed_by", adminUserId },
                { "p_payment_method", paymentMethod }
            };
            if (!string.IsNullOrWhiteSpace(adminNotes))
                rpcParams["p_admin_notes"] = adminNotes;

            var rpcResult = await _adminClient.Rpc(
                "process_affiliate_payout", rpcParams);

            var payload = ParseProcessPayoutRpc(rpcResult.Content);
            if (payload == null)
                return Response<AffiliatePayoutResultDTO>.Fail("Failed to process payout");

            if (!payload.Success)
            {
                return Response<AffiliatePayoutResultDTO>.Fail(
                    string.IsNullOrWhiteSpace(payload.Error)
                        ? "Failed to process payout"
                        : payload.Error!);
            }

            return Response<AffiliatePayoutResultDTO>.SuccessResponse(
                new AffiliatePayoutResultDTO
                {
                    PayoutId = payload.PayoutId ?? Guid.Empty,
                    AffiliateCode = payload.AffiliateCode ?? code,
                    AffiliateName = payload.AffiliateName ?? code,
                    TotalAmount = payload.TotalAmount,
                    ReferralCount = payload.ReferralCount,
                    ProcessedAt = payload.ProcessedAt ?? DateTime.UtcNow,
                    Status = payload.Status ?? "completed",
                    PaymentMethod = payload.PaymentMethod ?? paymentMethod
                },
                "Payout processed");
        }
        catch (Exception ex)
        {
            return Response<AffiliatePayoutResultDTO>.Fail("Error: " + ex.Message);
        }
    }

    private static ProcessPayoutRpcResult? ParseProcessPayoutRpc(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ProcessPayoutRpcResult>(
                content,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });
        }
        catch
        {
            return null;
        }
    }

    private sealed class ProcessPayoutRpcResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? PayoutId { get; set; }
        public string? AffiliateCode { get; set; }
        public string? AffiliateName { get; set; }
        public decimal TotalAmount { get; set; }
        public int ReferralCount { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public async Task<Response<PaginatedResponse<AffiliatePayoutResultDTO>>> GetAdminPayoutHistory(
        int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var offset = (page - 1) * pageSize;

            int totalCount;
            try
            {
                totalCount = await _adminClient
                    .From<AffiliatePayout>()
                    .Count(Supabase.Postgrest.Constants.CountType.Exact);
            }
            catch
            {
                var fallback = await _adminClient.From<AffiliatePayout>().Get();
                totalCount = fallback.Models.Count;
            }

            var pageResult = await _adminClient
                .From<AffiliatePayout>()
                .Order("processed_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            var pageItems = pageResult.Models;

            var codes = pageItems.Select(p => p.AffiliateCode).Distinct().ToList();
            var profiles = await FetchAffiliateProfilesByCodesAsync(codes);
            var nameByCode = profiles
                .Where(p => !string.IsNullOrWhiteSpace(p.AffiliateCode))
                .GroupBy(p => p.AffiliateCode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().FullName ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling((double)totalCount / pageSize);

            var data = pageItems.Select(p => new AffiliatePayoutResultDTO
            {
                PayoutId = p.Id,
                AffiliateCode = p.AffiliateCode,
                AffiliateName = nameByCode.TryGetValue(p.AffiliateCode, out var name)
                    ? name
                    : p.AffiliateCode,
                TotalAmount = p.TotalAmount,
                ReferralCount = p.ReferralCount,
                ProcessedAt = p.ProcessedAt,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod
            }).ToList();

            return Response<PaginatedResponse<AffiliatePayoutResultDTO>>.SuccessResponse(
                new PaginatedResponse<AffiliatePayoutResultDTO>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasMore = page < totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                },
                "Payout history fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<AffiliatePayoutResultDTO>>.Fail(
                "Error: " + ex.Message);
        }
    }

    private async Task<Profiles?> GetApprovedAffiliateByCodeAsync(string affiliateCode)
    {
        var result = await _adminClient
            .From<Profiles>()
            .Filter("affiliate_code", Supabase.Postgrest.Constants.Operator.Equals, affiliateCode)
            .Limit(1)
            .Get();

        var profile = result.Models.FirstOrDefault();
        if (profile == null)
            return null;

        if (!string.Equals(
                profile.AffiliateApplicationStatus, "approved", StringComparison.OrdinalIgnoreCase))
            return null;

        return profile;
    }

    private async Task<List<Profiles>> FetchAffiliateProfilesByCodesAsync(IEnumerable<string> codes)
    {
        var list = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
            return new List<Profiles>();

        var result = await _adminClient
            .From<Profiles>()
            .Filter("affiliate_code",
                Supabase.Postgrest.Constants.Operator.In,
                list.Select(c => (object)c).ToList())
            .Get();

        return result.Models;
    }

    private async Task<Dictionary<Guid, string>> FetchAffiliateTiersByUserIdsAsync(IEnumerable<Guid> userIds)
    {
        var enrichment = await BuildApplicationEnrichmentAsync(userIds);
        return enrichment.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.AffiliateTier,
            EqualityComparer<Guid>.Default);
    }

    private async Task<Dictionary<Guid, ApplicationEnrichment>> BuildApplicationEnrichmentAsync(
        IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        var result = new Dictionary<Guid, ApplicationEnrichment>();
        if (ids.Count == 0)
            return result;

        var profiles = (await _adminClient
            .From<Profiles>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.In,
                ids.Select(id => id.ToString()).ToList())
            .Get()).Models;

        var tiers = await GetCachedTiersAsync();
        var rateBySlug = tiers
            .GroupBy(t => t.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().CommissionRatePercent,
                StringComparer.OrdinalIgnoreCase);

        var codes = profiles
            .Where(p => !string.IsNullOrWhiteSpace(p.AffiliateCode))
            .Select(p => p.AffiliateCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lastSaleByCode = await FetchLastSaleByCodesAsync(codes);

        foreach (var profile in profiles.Where(p => p.Id.HasValue))
        {
            var slug = string.IsNullOrWhiteSpace(profile.AffiliateTier)
                ? "none"
                : profile.AffiliateTier.Trim().ToLowerInvariant();

            rateBySlug.TryGetValue(slug, out var rate);
            DateTime? lastSale = null;
            if (!string.IsNullOrWhiteSpace(profile.AffiliateCode)
                && lastSaleByCode.TryGetValue(profile.AffiliateCode!, out var saleAt))
            {
                lastSale = saleAt;
            }

            result[profile.Id!.Value] = new ApplicationEnrichment
            {
                AffiliateTier = string.IsNullOrWhiteSpace(profile.AffiliateTier)
                    ? "none"
                    : profile.AffiliateTier,
                ItemsSold = profile.AffiliateItemsSold,
                CommissionEarned = profile.AffiliateCommissionEarned,
                CommissionRatePercent = rate,
                LastSaleAt = lastSale,
                JoinDate = profile.AffiliateApprovedAt,
                IsActive = profile.AffiliateIsActive
            };
        }

        return result;
    }

    private async Task<Dictionary<string, DateTime>> FetchLastSaleByCodesAsync(
        IReadOnlyList<string> codes)
    {
        var map = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0)
            return map;

        var referrals = (await _adminClient
            .From<AffiliateReferral>()
            .Filter("affiliate_code",
                Supabase.Postgrest.Constants.Operator.In,
                codes.Select(c => (object)c).ToList())
            .Get()).Models;

        foreach (var group in referrals.GroupBy(r => r.AffiliateCode, StringComparer.OrdinalIgnoreCase))
            map[group.Key] = group.Max(r => r.CreatedAt);

        return map;
    }

    private async Task<Dictionary<string, int>> CountActiveAffiliatesByTierAsync()
    {
        var profiles = await FetchApprovedAffiliateProfilesAsync();
        return profiles
            .Where(p => p.AffiliateIsActive)
            .Where(p => !string.IsNullOrWhiteSpace(p.AffiliateTier)
                        && !p.AffiliateTier.Equals("none", StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.AffiliateTier.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<Profiles>> FetchApprovedAffiliateProfilesAsync()
    {
        var result = await _adminClient
            .From<Profiles>()
            .Filter("affiliate_application_status",
                Supabase.Postgrest.Constants.Operator.Equals,
                "approved")
            .Get();

        return result.Models;
    }

    private async Task<string?> EnsureTierHasCapacityAsync(string slug)
    {
        var normalized = NormalizeSlug(slug) ?? "bronze";
        var tier = await GetTierBySlugCachedAsync(normalized)
            ?? await FetchTierBySlugAsync(normalized);

        if (tier?.MaxAffiliates == null)
            return null;

        var counts = await CountActiveAffiliatesByTierAsync();
        var current = counts.GetValueOrDefault(normalized);
        if (current >= tier.MaxAffiliates.Value)
        {
            return $"Tier '{tier.DisplayName}' is full "
                + $"({current}/{tier.MaxAffiliates.Value} affiliates).";
        }

        return null;
    }

    private static string NormalizePaymentMethod(string? method)
    {
        var normalized = string.IsNullOrWhiteSpace(method)
            ? "manual"
            : method.Trim().ToLowerInvariant();

        return normalized switch
        {
            "paypal" => "paypal",
            "bank_transfer" => "bank_transfer",
            "store_credit" => "store_credit",
            _ => "manual"
        };
    }

    public async Task<Response<AffiliateAdminStatsDTO>> GetAdminStats()
    {
        try
        {
            var applications = (await _adminClient
                .From<AffiliateApplication>()
                .Get()).Models;

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var pendingApps = applications.Count(a =>
                a.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
            var waitlistedApps = applications.Count(a =>
                a.Status.Equals("waitlisted", StringComparison.OrdinalIgnoreCase));
            var appsThisMonth = applications.Count(a => a.SubmittedAt >= monthStart);

            var approvedProfiles = await FetchApprovedAffiliateProfilesAsync();
            var active = approvedProfiles.Count(p => p.AffiliateIsActive
                && !string.IsNullOrWhiteSpace(p.AffiliateTier)
                && !p.AffiliateTier.Equals("none", StringComparison.OrdinalIgnoreCase));
            var inactive = approvedProfiles.Count(p => !p.AffiliateIsActive
                && !string.IsNullOrWhiteSpace(p.AffiliateTier)
                && !p.AffiliateTier.Equals("none", StringComparison.OrdinalIgnoreCase));

            var byTier = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["bronze"] = 0,
                ["silver"] = 0,
                ["gold"] = 0,
                ["platinum"] = 0
            };
            foreach (var profile in approvedProfiles.Where(p => p.AffiliateIsActive))
            {
                var slug = (profile.AffiliateTier ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(slug) || slug == "none")
                    continue;
                byTier[slug] = byTier.GetValueOrDefault(slug) + 1;
            }

            var totalItemsSold = approvedProfiles.Sum(p => p.AffiliateItemsSold);

            var payouts = (await _adminClient.From<AffiliatePayout>().Get()).Models;
            var totalPaid = payouts.Sum(p => p.TotalAmount);
            var processedThisMonth = payouts.Where(p => p.ProcessedAt >= monthStart).ToList();
            var ytd = payouts.Where(p => p.ProcessedAt >= yearStart).Sum(p => p.TotalAmount);

            var pendingPayouts = await GetAdminPendingPayouts();
            var pendingList = pendingPayouts.Success
                ? pendingPayouts.Data ?? new List<AffiliatePendingPayoutDTO>()
                : new List<AffiliatePendingPayoutDTO>();

            return Response<AffiliateAdminStatsDTO>.SuccessResponse(
                new AffiliateAdminStatsDTO
                {
                    PendingApplications = pendingApps,
                    WaitlistedApplications = waitlistedApps,
                    ApplicationsThisMonth = appsThisMonth,
                    ActiveAffiliates = active,
                    InactiveAffiliates = inactive,
                    AffiliatesByTier = byTier,
                    TotalCommissionsPaid = totalPaid,
                    TotalItemsSold = totalItemsSold,
                    PendingPayoutAmount = pendingList.Sum(p => p.TotalAmount),
                    PendingPayoutCount = pendingList.Count,
                    ProcessedThisMonthAmount = processedThisMonth.Sum(p => p.TotalAmount),
                    ProcessedThisMonthCount = processedThisMonth.Count,
                    TotalDisbursedYtd = ytd
                },
                "Admin stats fetched");
        }
        catch (Exception ex)
        {
            return Response<AffiliateAdminStatsDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AffiliateApplicationDTO>> SetAffiliateActiveStatus(
        Guid userId, bool isActive)
    {
        try
        {
            if (userId == Guid.Empty)
                return Response<AffiliateApplicationDTO>.Fail("User id is required");

            var profile = (await _adminClient
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Limit(1)
                .Get()).Models.FirstOrDefault();

            if (profile == null)
                return Response<AffiliateApplicationDTO>.Fail("Affiliate profile not found");

            if (!string.Equals(
                    profile.AffiliateApplicationStatus, "approved", StringComparison.OrdinalIgnoreCase))
            {
                return Response<AffiliateApplicationDTO>.Fail(
                    "Only approved affiliates can be activated or deactivated");
            }

            var updated = (await _adminClient
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Set(p => p.AffiliateIsActive!, isActive)
                .Update()).Models.FirstOrDefault();

            if (updated == null)
                return Response<AffiliateApplicationDTO>.Fail("Failed to update affiliate status");

            var application = (await _adminClient
                .From<AffiliateApplication>()
                .Where(a => a.UserId == userId)
                .Order(a => a.SubmittedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get()).Models.FirstOrDefault();

            if (application == null)
            {
                var enrichment = await BuildApplicationEnrichmentAsync(new[] { userId });
                var e = enrichment.GetValueOrDefault(userId) ?? new ApplicationEnrichment();
                return Response<AffiliateApplicationDTO>.SuccessResponse(
                    new AffiliateApplicationDTO
                    {
                        UserId = userId,
                        FullName = updated.FullName ?? string.Empty,
                        Email = updated.Email ?? string.Empty,
                        Status = "approved",
                        AffiliateTier = e.AffiliateTier,
                        ItemsSold = e.ItemsSold,
                        CommissionEarned = e.CommissionEarned,
                        CommissionRatePercent = e.CommissionRatePercent,
                        LastSaleAt = e.LastSaleAt,
                        JoinDate = e.JoinDate ?? updated.AffiliateApprovedAt ?? DateTime.UtcNow,
                        IsActive = updated.AffiliateIsActive,
                        SubmittedAt = updated.AffiliateApprovedAt ?? DateTime.UtcNow
                    },
                    isActive ? "Affiliate activated" : "Affiliate deactivated");
            }

            var map = await BuildApplicationEnrichmentAsync(new[] { userId });
            return Response<AffiliateApplicationDTO>.SuccessResponse(
                MapToDTO(application, map.GetValueOrDefault(userId)),
                isActive ? "Affiliate activated" : "Affiliate deactivated");
        }
        catch (Exception ex)
        {
            return Response<AffiliateApplicationDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<BulkAffiliatePayoutResultDTO>> ProcessAllAdminPayouts(
        BulkProcessAffiliatePayoutsDTO request, Guid adminUserId)
    {
        try
        {
            if (adminUserId == Guid.Empty)
                return Response<BulkAffiliatePayoutResultDTO>.Fail("Not authenticated");

            var pendingResult = await GetAdminPendingPayouts();
            if (!pendingResult.Success)
            {
                return Response<BulkAffiliatePayoutResultDTO>.Fail(
                    pendingResult.Message ?? "Failed to load pending payouts");
            }

            var pending = pendingResult.Data ?? new List<AffiliatePendingPayoutDTO>();
            var processRequest = new ProcessAffiliatePayoutDTO
            {
                PaymentMethod = request?.PaymentMethod,
                AdminNotes = request?.AdminNotes
            };

            var result = new BulkAffiliatePayoutResultDTO();
            foreach (var item in pending)
            {
                var processed = await ProcessAdminPayout(
                    item.AffiliateCode, processRequest, adminUserId);
                if (!processed.Success || processed.Data == null)
                {
                    result.FailedCodes.Add(item.AffiliateCode);
                    continue;
                }

                result.ProcessedCount++;
                result.TotalAmount += processed.Data.TotalAmount;
                result.PayoutIds.Add(processed.Data.PayoutId);
            }

            return Response<BulkAffiliatePayoutResultDTO>.SuccessResponse(
                result,
                result.FailedCodes.Count == 0
                    ? "All payouts processed"
                    : $"Processed {result.ProcessedCount} payouts; {result.FailedCodes.Count} failed");
        }
        catch (Exception ex)
        {
            return Response<BulkAffiliatePayoutResultDTO>.Fail("Error: " + ex.Message);
        }
    }

    private async Task<Dictionary<Guid, string>> FetchOrderNumbersAsync(IReadOnlyList<Guid> orderIds)
    {
        if (orderIds.Count == 0)
            return new Dictionary<Guid, string>();

        var result = await _adminClient
            .From<Order>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.In,
                orderIds.Select(id => id.ToString()).ToList())
            .Get();

        return result.Models
            .Where(o => !string.IsNullOrWhiteSpace(o.OrderNumber))
            .ToDictionary(o => o.Id, o => o.OrderNumber!);
    }

}
