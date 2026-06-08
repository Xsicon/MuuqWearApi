using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.AffiliatePerfomanceDTO;
using MuuqWear.Model.DTO.RevenueOverTimeDTO;
using MuuqWear.Model.DTO.TopSellingProductDTO;

namespace MuuqWear.Application.Interfaces;

public interface IAnalyticsService
{
    Task<Response<RevenueOverTimeDTO>> GetRevenueOverTime();
    Task<Response<List<TopSellingProductDTO>>> GetTopSellingProducts(int limit = 5);
    Task<Response<List<AffiliatePerformanceDTO>>> GetAffiliatePerformance();
}
