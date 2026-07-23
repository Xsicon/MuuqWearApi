using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminBadgeCount;
using MuuqWear.Model.Models.AffiliateApplication;
using MuuqWear.Model.Models.Chat;
using MuuqWear.Model.Models.Profiles;
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
            var pendingOrders = CountOrders();
            var totalCustomers = CountCustomers();
            var totalProducts = CountProducts();
            var affiliateCounts = CountAffiliateApplications();
            var openTickets = CountOpenTickets();
            var pendingPayouts = CountPendingPayoutAffiliates();
            var activeChats = CountWaitingChats();

            await Task.WhenAll(
                pendingOrders, totalCustomers, totalProducts,
                affiliateCounts, openTickets, pendingPayouts, activeChats);

            var affiliate = affiliateCounts.Result;
            affiliate.PendingPayouts = pendingPayouts.Result;

            var dto = new AdminBadgeCountsDTO
            {
                PendingOrders = pendingOrders.Result,
                TotalCustomers = totalCustomers.Result,
                TotalProducts = totalProducts.Result,
                AffiliateCounts = affiliate,
                OpenTickets = openTickets.Result,
                ActiveChats = activeChats.Result
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
            { "p_max_price", 999999 },
            {"p_include_tickets",false }
        });

    private Task<int> CountOpenTickets() => CallRpc("get_support_tickets_count",
        new Dictionary<string, object>
        {
            { "p_status", "open" }
        });

    private async Task<int> CountWaitingChats()
    {
        try
        {
            var sessionsResult = await _adminClient
                .From<ChatSession>()
                .Where(s => s.Status == "active")
                .Get();

            var sessions = sessionsResult.Models ?? new List<ChatSession>();
            if (sessions.Count == 0)
                return 0;

            var latestMessageBySession = await ChatMessageQueryHelper.LoadLatestMessagesBySessionAsync(
                _adminClient,
                sessions.Select(s => s.Id).ToList());

            return ChatMessageQueryHelper.CountWaitingSessions(sessions, latestMessageBySession);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminBadge] CountWaitingChats error: {ex.Message}");
            return 0;
        }
    }

    private async Task<int> CountPendingPayoutAffiliates()
    {
        try
        {
            var result = await _adminClient.Rpc(
                "count_affiliate_pending_payout_affiliates", null);
            var content = result.Content?.Trim('"') ?? "0";
            if (int.TryParse(content, out var value))
                return value;

            // Fallback if RPC not deployed yet
            return await CountPendingPayoutAffiliatesFallbackAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminBadge] CountPendingPayoutAffiliates error: {ex.Message}");
            try
            {
                return await CountPendingPayoutAffiliatesFallbackAsync();
            }
            catch
            {
                return 0;
            }
        }
    }

    private async Task<int> CountPendingPayoutAffiliatesFallbackAsync()
    {
        var pending = await _adminClient
            .From<AffiliateReferral>()
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "pending")
            .Get();

        var codes = pending.Models
            .Where(r => r.CommissionAmount > 0)
            .Select(r => r.AffiliateCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
            return 0;

        var profiles = await _adminClient
            .From<Profiles>()
            .Filter("affiliate_code",
                Supabase.Postgrest.Constants.Operator.In,
                codes.Select(c => (object)c).ToList())
            .Get();

        var approved = profiles.Models
            .Where(p => string.Equals(
                p.AffiliateApplicationStatus, "approved", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(p.AffiliateCode))
            .Select(p => p.AffiliateCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return codes.Count(c => approved.Contains(c));
    }

    private async Task<AffiliateCountsDTO> CountAffiliateApplications()
    {
        try
        {
            var rows = await CallRpcArray<StatusCountRow>(
                "get_affiliate_application_counts", null);

            var counts = new AffiliateCountsDTO();

            foreach (var row in rows)
            {
                switch (row.Status)
                {
                    case "pending": counts.Pending = row.Count; break;
                    case "approved": counts.Approved = row.Count; break;
                    case "rejected": counts.Rejected = row.Count; break;
                    case "waitlisted": counts.Waitlisted = row.Count; break;
                }
            }

            return counts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminBadge] CountAffiliateApplications error: {ex.Message}");
            return new AffiliateCountsDTO();
        }
    }

    private class StatusCountRow
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private async Task<int> CallRpc(string functionName, Dictionary<string, object>? parameters)
    {
        var result = await _adminClient.Rpc(functionName, parameters);
        var content = result.Content?.Trim('"') ?? "0";
        return int.TryParse(content, out var value) ? value : 0;
    }

    private async Task<List<T>> CallRpcArray<T>(
    string functionName, Dictionary<string, object>? parameters)
    {
        var result = await _adminClient.Rpc(functionName, parameters);
        if (result?.Content == null) return new List<T>();

        return System.Text.Json.JsonSerializer
            .Deserialize<List<T>>(result.Content,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            ?? new List<T>();
    }
}
