using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.NotificationDTO;
using MuuqWear.Model.Models.AffiliateApplication;
using MuuqWear.Model.Models.Order;
using MuuqWear.Model.Models.Product;
using MuuqWear.Model.Models.SupportTicket;

namespace MuuqWear.Application.Service;

public class NotificationService : INotificationService
{
    private const int LowStockThreshold = 5;
    private const int LowStockFetchLimit = 10;

    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;

    public NotificationService(
        SupabaseClientFactory factory,
        SupabaseAdminClientFactory adminFactory)
    {
        _client = factory.CreateClient();
        _adminClient = adminFactory.CreateClient();
    }

    public async Task<Response<List<NotificationDTO>>> GetRecent()
    {
        try
        {
            var notifications = new List<NotificationDTO>();

            //  run all queries in parallel
            await Task.WhenAll(
                FetchOrderNotifications(notifications),
                FetchTicketNotifications(notifications),
                FetchReturnNotifications(notifications),
                FetchLowStockNotifications(notifications),
                FetchAffiliateApplicationNotifications(notifications));

            //  sort by date descending → take 5
            var result = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            return Response<List<NotificationDTO>>
                .SuccessResponse(result, "Notifications fetched");
        }
        catch (Exception ex)
        {
            return Response<List<NotificationDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // ─── ORDERS ───────────────────────────────────────────────
    private async Task FetchOrderNotifications(
        List<NotificationDTO> notifications)
    {
        var orders = await _client
            .From<Order>()
            .Order("created_at",
                Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(5)
            .Get();

        foreach (var order in orders.Models)
        {
            notifications.Add(new NotificationDTO
            {
                Id = order.Id,
                Type = "order",
                Message = $"New order #{order.OrderNumber} placed",
                CreatedAt = order.CreatedAt ?? DateTime.UtcNow
            });
        }
    }

    // ─── TICKETS ──────────────────────────────────────────────
    private async Task FetchTicketNotifications(
        List<NotificationDTO> notifications)
    {
        var tickets = await _client
            .From<SupportTicket>()
            .Order("created_at",
                Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(5)
            .Get();

        foreach (var ticket in tickets.Models)
        {
            notifications.Add(new NotificationDTO
            {
                Id = ticket.Id,
                Type = "ticket",
                Message = $"New support ticket: {ticket.Subject}",
                CreatedAt = ticket.CreatedAt ?? DateTime.UtcNow
            });
        }
    }

    // ─── RETURNS ──────────────────────────────────────────────
    private async Task FetchReturnNotifications(
        List<NotificationDTO> notifications)
    {
        var returns = await _client
            .From<OrderReturn>()
            .Order("created_at",
                Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(5)
            .Get();

        foreach (var ret in returns.Models)
        {
            notifications.Add(new NotificationDTO
            {
                Id = ret.Id,
                Type = "return",
                Message = $"Return request #{ret.ReturnNumber} submitted",
                CreatedAt = ret.CreatedAt ?? DateTime.UtcNow
            });
        }
    }

    // ─── LOW STOCK ────────────────────────────────────────────
    //  quantity < 5 → low stock alert
    private async Task FetchLowStockNotifications(
        List<NotificationDTO> notifications)
    {
        var lowStock = await _adminClient
            .From<ProductSizeStock>()
            .Filter("quantity",
                Supabase.Postgrest.Constants.Operator.LessThan,
                LowStockThreshold.ToString())
            .Filter("quantity",
                Supabase.Postgrest.Constants.Operator.GreaterThan,
                "0")
            .Order("quantity",
                Supabase.Postgrest.Constants.Ordering.Ascending)
            .Limit(LowStockFetchLimit)
            .Get();

        if (lowStock.Models.Count == 0)
            return;

        var productIds = lowStock.Models
            .Select(s => s.ProductId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var productNames = await FetchProductNamesAsync(productIds);

        foreach (var stock in lowStock.Models)
        {
            if (!productNames.TryGetValue(stock.ProductId, out var productName))
                continue;

            var sizeLabel = stock.Size.Trim();

            notifications.Add(new NotificationDTO
            {
                Id = stock.Id,
                Type = "low_stock",
                Message = BuildLowStockMessage(
                    productName, sizeLabel, stock.Quantity),
                ProductId = stock.ProductId,
                SizeLabel = sizeLabel,
                SizeStockId = stock.Id,
                Link = $"/admin/products?view=low-stock&productId={stock.ProductId}",
                CreatedAt = stock.CreatedAt ?? DateTime.UtcNow
            });
        }
    }

    private async Task<Dictionary<Guid, string>> FetchProductNamesAsync(
        IReadOnlyList<Guid> productIds)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, string>();

        var idFilters = productIds
            .Select(id => (object)id.ToString())
            .ToList();

        var products = await _adminClient
            .From<Product>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.In,
                idFilters)
            .Filter("is_deleted",
                Supabase.Postgrest.Constants.Operator.Equals,
                "false")
            .Get();

        return products.Models.ToDictionary(
            p => p.Id,
            p => string.IsNullOrWhiteSpace(p.Name) ? "Product" : p.Name.Trim());
    }

    private static string BuildLowStockMessage(
        string productName, string sizeLabel, int quantity)
    {
        return $"Low stock alert: {productName} (Size {sizeLabel}) — {quantity} left";
    }

    public async Task<Response<List<NotificationDTO>>> GetRecent(
    DateTime? lastReadAt)
    {
        try
        {
            var notifications = new List<NotificationDTO>();

            await Task.WhenAll(
                FetchOrderNotifications(notifications),
                FetchTicketNotifications(notifications),
                FetchReturnNotifications(notifications),
                FetchLowStockNotifications(notifications),
                FetchAffiliateApplicationNotifications(notifications));

            var result = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            //  mark as read based on lastReadAt
            if (lastReadAt.HasValue)
            {
                foreach (var notif in result)
                    notif.IsRead = notif.CreatedAt <= lastReadAt.Value;
            }

            return Response<List<NotificationDTO>>
                .SuccessResponse(result, "Notifications fetched");
        }
        catch (Exception ex)
        {
            return Response<List<NotificationDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    private async Task FetchAffiliateApplicationNotifications(
    List<NotificationDTO> notifications)
    {
        var applications = await _client
            .From<AffiliateApplication>()
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "pending")
            .Order("submitted_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(5)
            .Get();

        foreach (var app in applications.Models)
        {
            notifications.Add(new NotificationDTO
            {
                Id = app.Id,
                Type = "affiliate_application",
                Message = $"New affiliate application from {app.FullName}",
                CreatedAt = app.SubmittedAt
            });
        }
    }
}
