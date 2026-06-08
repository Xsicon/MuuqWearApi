using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AffiliatePerfomanceDTO;
using MuuqWear.Model.DTO.RevenueOverTimeDTO;
using MuuqWear.Model.DTO.TopSellingProductDTO;

namespace MuuqWear.Application.Service;

public class AnalyticsService : IAnalyticsService
{
    private readonly Supabase.Client _client;

    public AnalyticsService(SupabaseAdminClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // REVENUE OVER TIME — main chart + headline number
    // =============================================
    public async Task<Response<RevenueOverTimeDTO>> GetRevenueOverTime()
    {
        try
        {
            // Two RPCs in parallel: daily points + summary totals
            var dailyTask = CallRpcArray<DailyRevenueRow>(
                "get_revenue_over_time", null);
            var summaryTask = CallRpcArray<RevenueSummaryRow>(
                "get_revenue_summary", null);

            await Task.WhenAll(dailyTask, summaryTask);

            var dailyRows = dailyTask.Result;
            var summary = summaryTask.Result.FirstOrDefault()
                          ?? new RevenueSummaryRow();

            var dto = new RevenueOverTimeDTO
            {
                CurrentTotal = summary.CurrentTotal,
                PreviousTotal = summary.PreviousTotal,
                DailyRevenue = dailyRows.Select(r => new DailyRevenueDTO
                {
                    Day = r.Day,
                    Revenue = r.Revenue
                }).ToList()
            };

            return Response<RevenueOverTimeDTO>.SuccessResponse(
                dto, "Revenue fetched");
        }
        catch (Exception ex)
        {
            return Response<RevenueOverTimeDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // TOP SELLING PRODUCTS
    // =============================================
    public async Task<Response<List<TopSellingProductDTO>>> GetTopSellingProducts(
        int limit = 5)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "p_limit", limit }
            };

            var rows = await CallRpcArray<TopProductRow>(
                "get_top_selling_products", parameters);

            var dtos = rows.Select(r => new TopSellingProductDTO
            {
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                UnitsSold = r.UnitsSold,
                Revenue = r.Revenue
            }).ToList();

            return Response<List<TopSellingProductDTO>>.SuccessResponse(
                dtos, "Top products fetched");
        }
        catch (Exception ex)
        {
            return Response<List<TopSellingProductDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // AFFILIATE PERFORMANCE BY TIER
    // =============================================
    public async Task<Response<List<AffiliatePerformanceDTO>>> GetAffiliatePerformance()
    {
        try
        {
            var rows = await CallRpcArray<AffiliateTierRow>(
                "get_affiliate_performance", null);

            var dtos = rows.Select(r => new AffiliatePerformanceDTO
            {
                Tier = r.Tier,
                Sales = r.Sales,
                Commission = r.Commission,
                AffiliateCount = r.AffiliateCount
            }).ToList();

            return Response<List<AffiliatePerformanceDTO>>.SuccessResponse(
                dtos, "Affiliate performance fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AffiliatePerformanceDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // RPC HELPER
    // =============================================
    private async Task<List<T>> CallRpcArray<T>(
        string functionName, Dictionary<string, object>? parameters)
    {
        var result = await _client.Rpc(functionName, parameters);
        if (result?.Content == null) return new List<T>();

        return System.Text.Json.JsonSerializer
            .Deserialize<List<T>>(result.Content,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                })
            ?? new List<T>();
    }

    // =============================================
    // INTERNAL JSON DESERIALIZATION HELPERS
    // =============================================
    private class DailyRevenueRow
    {
        public DateTime Day { get; set; }
        public decimal Revenue { get; set; }
    }

    private class RevenueSummaryRow
    {
        public decimal CurrentTotal { get; set; }
        public decimal PreviousTotal { get; set; }
    }

    private class TopProductRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    private class AffiliateTierRow
    {
        public string Tier { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Commission { get; set; }
        public int AffiliateCount { get; set; }
    }
}
