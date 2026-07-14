using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.AffiliateApplicationDTO;
using MuuqWear.Model.DTO.PartnerStoreProductDTO;

namespace MuuqWear.Application.Interfaces;

public interface IAffiliateService
{
    // User operations
    Task<Response<AffiliateApplicationDTO>> SubmitApplication(
        Guid userId, SubmitAffiliateApplicationDTO request);

    Task<Response<AffiliateStatusDTO>> GetUserAffiliateStatus(Guid userId);

    Task<Response<AffiliateApplicationDTO?>> GetUserApplication(Guid userId);

    // Admin operations
    Task<Response<List<AffiliateApplicationDTO>>> GetAllApplications(
        string? statusFilter = null);

    Task<Response<AffiliateApplicationDTO>> UpdateApplicationStatus(
        Guid applicationId, string status, Guid reviewedBy, string? adminNotes = null);

    Task<Response<int>> GetPendingCount();
    Task<Response<int>> GetSpotsRemaining();
    Task<Response<bool>> ApproveApplication(Guid applicationId);
    Task<Response<AffiliateInfoDTO>> GetAffiliateInfo(Guid userId);
    Task<Response<bool>> ValidateAffiliateCode(string affiliateCode);
    Task<Response<bool>> TrackClick(TrackClickRequestDTO request);
    Task<Response<bool>> HasRecentClick(string affiliateCode, string ipAddress);
    Task<Response<decimal>> GetCommissionRate(string affiliateCode);
    Task<Response<bool>> TrackOrderReferral(Guid orderId, Guid userId, decimal orderTotal, string? affiliateCode);
    Task<Response<PerformanceChartDTO>> GetPerformanceChart(string affiliateCode);
    Task<Response<PaginatedResponse<PartnerStoreProductDTO>>> GetPartnerStoreProducts(
        Guid userId,
        int page = 1,
        int pageSize = 15); Task<Response<AffiliatePurchaseLimitDTO>> GetPurchaseLimitStatus(Guid userId);
    Task<Response<bool>> CanPurchase(Guid userId, int quantity);
    Task<Response<List<RecentReferralDTO>>> GetRecentReferrals(Guid userId);

    // Tier settings
    Task<Response<List<AffiliateTierDTO>>> GetAdminTiers();
    Task<Response<AffiliateTierDTO>> GetAdminTierBySlug(string slug);
    Task<Response<AffiliateTierDTO>> UpdateAdminTier(
        string slug, UpdateAffiliateTierDTO request, Guid adminUserId);
    Task<Response<List<AffiliateTierDTO>>> GetPublicTiers();

    // Admin dashboard
    Task<Response<AffiliateAdminStatsDTO>> GetAdminStats();
    Task<Response<AffiliateApplicationDTO>> SetAffiliateActiveStatus(
        Guid userId, bool isActive);

    // Admin payouts
    Task<Response<List<AffiliatePendingPayoutDTO>>> GetAdminPendingPayouts();
    Task<Response<List<AffiliatePendingReferralDTO>>> GetAdminPendingReferrals(string affiliateCode);
    Task<Response<AffiliatePayoutResultDTO>> ProcessAdminPayout(
        string affiliateCode, ProcessAffiliatePayoutDTO request, Guid adminUserId);
    Task<Response<BulkAffiliatePayoutResultDTO>> ProcessAllAdminPayouts(
        BulkProcessAffiliatePayoutsDTO request, Guid adminUserId);
    Task<Response<PaginatedResponse<AffiliatePayoutResultDTO>>> GetAdminPayoutHistory(
        int page = 1, int pageSize = 20);
}
