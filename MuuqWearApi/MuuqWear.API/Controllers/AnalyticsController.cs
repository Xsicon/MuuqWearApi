using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.AffiliatePerfomanceDTO;
using MuuqWear.Model.DTO.RevenueOverTimeDTO;
using MuuqWear.Model.DTO.TopSellingProductDTO;

namespace MuuqWear.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AnalyticsController : BaseController
{
    private readonly IAnalyticsService _service;

    public AnalyticsController(IAnalyticsService service)
    {
        _service = service;
    }

    // =============================================
    // REVENUE OVER TIME — chart + headline
    // =============================================
    [HttpGet("revenue")]
    public async Task<ActionResult<Response<RevenueOverTimeDTO>>> GetRevenue()
    {
        var result = await _service.GetRevenueOverTime();
        return HandleResponse(result);
    }

    // =============================================
    // TOP SELLING PRODUCTS
    // =============================================
    [HttpGet("top-products")]
    public async Task<ActionResult<Response<List<TopSellingProductDTO>>>> GetTopProducts(
        [FromQuery] int limit = 5)
    {
        // Cap the limit to prevent abuse (e.g., ?limit=10000)
        if (limit < 1) limit = 5;
        if (limit > 50) limit = 50;

        var result = await _service.GetTopSellingProducts(limit);
        return HandleResponse(result);
    }

    // =============================================
    // AFFILIATE PERFORMANCE
    // =============================================
    [HttpGet("affiliate-performance")]
    public async Task<ActionResult<Response<List<AffiliatePerformanceDTO>>>> GetAffiliatePerformance()
    {
        var result = await _service.GetAffiliatePerformance();
        return HandleResponse(result);
    }
}
