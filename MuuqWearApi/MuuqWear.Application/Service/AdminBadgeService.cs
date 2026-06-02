using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminBadgeCount;
using Supabase;

namespace MuuqWear.Application.Service;
public class AdminBadgeService : IAdminBadgeService
{
    private readonly Client _adminClient;

    public AdminBadgeService(SupabaseAdminClientFactory adminFactory)
    {
        _adminClient = adminFactory.CreateClient();
    }

    public async Task<Response<AdminBadgeCountsDTO>> GetCounts()
    {
        try
        {
            // Six independent count queries — parallelize
            var pendingOrders = CountOrders();
            var totalCustomers = CountCustomers();
            var totalProducts = CountProducts();
            //var pendingApps = CountPendingApplications();
            //var activeChats = CountActiveChats();
            var openTickets = CountOpenTickets();

            await Task.WhenAll(
                pendingOrders, totalCustomers, totalProducts,
                 openTickets);

            var dto = new AdminBadgeCountsDTO
            {
                PendingOrders = pendingOrders.Result,
                TotalCustomers = totalCustomers.Result,
                TotalProducts = totalProducts.Result,
                OpenTickets = openTickets.Result
            };

            return Response<AdminBadgeCountsDTO>.SuccessResponse(
                dto, "Counts fetched successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminBadge] GetCounts error: {ex.Message}");
            return Response<AdminBadgeCountsDTO>.Fail($"Error: {ex.Message}");
        }
    }

    // ── individual count queries ──────────────────────────

    private Task<int> CountOrders() => CallRpc("get_orders_count",
        new Dictionary<string, object>
        {
            { "p_status", "pending" },
            { "p_search_term", "" }
        });

    private Task<int> CountCustomers() => CallRpc("get_customers_count",
        new Dictionary<string, object>
        {
            { "p_search_term", "" }
        });

    private Task<int> CountProducts() => CallRpc("get_products_count",
        new Dictionary<string, object>
        {
            { "p_search_term", "" },
            { "p_category_id", null! },
            { "p_size_filter", "" },
            { "p_min_price", 0 },
            { "p_max_price", 999999 }
        });

    //private Task<int> CountPendingApplications() =>
    //    CallRpc("count_pending_affiliate_applications", null);

    //private Task<int> CountActiveChats() =>
    //    CallRpc("count_active_chats", null);

    private Task<int> CountOpenTickets() => CallRpc("get_support_tickets_count",
        new Dictionary<string, object>
        {
            { "p_status", "open" }
        });

    /// <summary>
    /// Calls a SQL function that returns a single int. Returns 0 on parse
    /// failure — defensive for navbar resilience.
    /// </summary>
    private async Task<int> CallRpc(string functionName, Dictionary<string, object>? parameters)
    {
        var result = await _adminClient.Rpc(functionName, parameters);
        var content = result.Content?.Trim('"') ?? "0";
        return int.TryParse(content, out var value) ? value : 0;
    }
}
