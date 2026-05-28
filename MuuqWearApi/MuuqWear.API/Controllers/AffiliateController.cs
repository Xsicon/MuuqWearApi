using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.AffiliateApplicationDTO;
using MuuqWear.Model.DTO.PartnerStoreProductDTO;

namespace MuuqWear.Application.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AffiliateController : BaseController
{
    private readonly IAffiliateService _affiliateService;
    private readonly Supabase.Client _supabaseClient;


    public AffiliateController(
          IAffiliateService affiliateService,
          SupabaseClientFactory factory)
    {
        _affiliateService = affiliateService;
        _supabaseClient = factory.CreateClient();  //  Create client from factory
    }

    // =============================================
    // USER ENDPOINTS
    // =============================================

    /// <summary>
    /// Submit affiliate application
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<Response<AffiliateApplicationDTO>>> SubmitApplication(
        [FromBody] SubmitAffiliateApplicationDTO request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Email is required"));
        if (request.SocialHandles == null || !request.SocialHandles.Any())
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "At least one social media handle is required"));

        if (request.AudienceSize < 100)
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Minimum audience size is 100 followers"));

        if (string.IsNullOrWhiteSpace(request.ContentNiche))
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Content niche is required"));

        var userId = GetUserId();
        var result = await _affiliateService.SubmitApplication(userId, request);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get current user's affiliate status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<Response<AffiliateStatusDTO>>> GetStatus()
    {
        var userId = GetUserId();
        var result = await _affiliateService.GetUserAffiliateStatus(userId);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get current user's application details
    /// </summary>
    [HttpGet("application")]
    public async Task<ActionResult<Response<AffiliateApplicationDTO?>>> GetMyApplication()
    {
        var userId = GetUserId();
        var result = await _affiliateService.GetUserApplication(userId);
        return HandleResponse(result);
    }

    // =============================================
    // ADMIN ENDPOINTS
    // =============================================

    /// <summary>
    /// Get all affiliate applications (Admin only)
    /// </summary>
    /// <param name="status">Optional status filter: pending, approved, rejected, waitlisted</param>
    [HttpGet("admin/applications")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<List<AffiliateApplicationDTO>>>> GetAllApplications(
        [FromQuery] string? status = null)
    {
        // Validate status if provided
        if (!string.IsNullOrEmpty(status))
        {
            var validStatuses = new[] { "pending", "approved", "rejected", "waitlisted" };
            if (!validStatuses.Contains(status.ToLower()))
                return BadRequest(Response<List<AffiliateApplicationDTO>>.Fail(
                    "Invalid status. Valid values: pending, approved, rejected, waitlisted"));
        }

        var result = await _affiliateService.GetAllApplications(status);
        return HandleResponse(result);
    }

    /// <summary>
    /// Update application status (Admin only)
    /// </summary>
    [HttpPut("admin/applications/{applicationId}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<AffiliateApplicationDTO>>> UpdateApplicationStatus(
        Guid applicationId,
        [FromBody] UpdateApplicationStatusDTO request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Status is required"));

        var validStatuses = new[] { "pending", "approved", "rejected", "waitlisted" };
        if (!validStatuses.Contains(request.Status.ToLower()))
            return BadRequest(Response<AffiliateApplicationDTO>.Fail(
                "Invalid status. Valid values: pending, approved, rejected, waitlisted"));

        var adminUserId = GetUserId();
        var result = await _affiliateService.UpdateApplicationStatus(
            applicationId,
            request.Status,
            adminUserId,
            request.AdminNotes);

        return HandleResponse(result);
    }

    /// <summary>
    /// Get count of pending applications (Admin only)
    /// </summary>
    [HttpGet("admin/pending-count")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<int>>> GetPendingCount()
    {
        var result = await _affiliateService.GetPendingCount();
        return HandleResponse(result);
    }

    /// <summary>
    /// Get number of spots remaining (public endpoint)
    /// </summary>
    [HttpGet("spots-remaining")]
    [AllowAnonymous]  //  Public endpoint
    public async Task<ActionResult<Response<int>>> GetSpotsRemaining()
    {
        var result = await _affiliateService.GetSpotsRemaining();
        return HandleResponse(result);
    }

    [HttpPost("admin/approve/{applicationId}")]
    // TODO: Add [Authorize(Roles = "Admin")] when role system is ready
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> ApproveApplication(Guid applicationId)
    {
        var result = await _affiliateService.ApproveApplication(applicationId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    // =============================================
    // USER ENDPOINTS
    // =============================================

    /// <summary>
    /// Get affiliate dashboard info (approved affiliates only)
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<Response<AffiliateInfoDTO>>> GetAffiliateInfo()
    {
        var userId = GetUserId();
        var result = await _affiliateService.GetAffiliateInfo(userId);
        return HandleResponse(result);
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCode(string code)
    {
        var result = await _affiliateService.ValidateAffiliateCode(code);
        return Ok(result);
    }

    [HttpPost("track-click")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackClick([FromBody] TrackClickRequestDTO request)
    {
        var result = await _affiliateService.TrackClick(request);
        return Ok(result);
    }
    [HttpGet("check-recent-click/{code}/{ipAddress}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckRecentClick(string code, string ipAddress)
    {
        var result = await _affiliateService.HasRecentClick(code, ipAddress);
        return Ok(result);
    }
    [HttpGet("test/commission-rate/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> TestCommissionRate(string code)
    {
        var result = await _affiliateService.GetCommissionRate(code);
        return Ok(result);
    }


    [HttpGet("performance-chart")]
    [Authorize]
    public async Task<IActionResult> GetPerformanceChart()
    {
        try
        {
            // Get affiliate code from current user
            var affiliateCode = await GetAffiliateCodeFromUser();

            if (string.IsNullOrEmpty(affiliateCode))
            {
                return Ok(Response<PerformanceChartDTO>.Fail("No affiliate account found"));
            }

            var result = await _affiliateService.GetPerformanceChart(affiliateCode);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Controller] Chart error: {ex.Message}");
            return Ok(Response<PerformanceChartDTO>.Fail($"Error: {ex.Message}"));
        }
    }
    private async Task<string?> GetAffiliateCodeFromUser()
    {
        // Get user ID from claims
        var userId = GetUserId();

        try
        {
            // Query profile to get affiliate code
            var profile = await _supabaseClient
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Single();

            return profile?.AffiliateCode;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get partner store products with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 15)</param>
    [HttpGet("partner-store/products")]
    public async Task<ActionResult<Response<PaginatedResponse<PartnerStoreProductDTO>>>> GetPartnerStoreProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        try
        {
            var userId = GetUserId();
            var result = await _affiliateService.GetPartnerStoreProducts(userId, page, pageSize);
            return HandleResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Controller] GetPartnerStoreProducts error: {ex.Message}");
            return HandleResponse(Response<PaginatedResponse<PartnerStoreProductDTO>>.Fail(
                "Failed to load products"));
        }
    }

    /// <summary>
    /// Get affiliate's monthly purchase limit status
    /// </summary>
    [HttpGet("partner-store/purchase-limit")]
    public async Task<ActionResult<Response<AffiliatePurchaseLimitDTO>>> GetPurchaseLimit()
    {
        try
        {
            var userId = GetUserId();
            var result = await _affiliateService.GetPurchaseLimitStatus(userId);
            return HandleResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Controller] GetPurchaseLimit error: {ex.Message}");
            return HandleResponse(Response<AffiliatePurchaseLimitDTO>.Fail(
                "Failed to get limit status"));
        }
    }

    /// <summary>
    /// Validate if affiliate can purchase the requested quantity
    /// </summary>
    [HttpGet("partner-store/can-purchase/{quantity}")]
    public async Task<ActionResult<Response<bool>>> CanPurchaseQuantity(int quantity)
    {
        try
        {
            var userId = GetUserId();
            var result = await _affiliateService.CanPurchase(userId, quantity);
            return HandleResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Controller] CanPurchase error: {ex.Message}");
            return HandleResponse(Response<bool>.Fail(
                "Failed to validate purchase"));
        }
    }

    /// <summary>
    /// Get recent referrals for affiliate dashboard
    /// </summary>
    [HttpGet("recent-referrals")]
    public async Task<ActionResult<Response<List<RecentReferralDTO>>>> GetRecentReferrals()
    {
        try
        {
            var userId = GetUserId();
            var result = await _affiliateService.GetRecentReferrals(userId);
            return HandleResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [Controller] GetRecentReferrals error: {ex.Message}");
            return HandleResponse(Response<List<RecentReferralDTO>>.Fail(
                "Failed to load recent referrals"));
        }
    }
}
