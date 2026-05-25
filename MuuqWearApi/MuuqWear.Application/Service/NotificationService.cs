using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.NotificationDTO;
using MuuqWear.Model.Models;

namespace MuuqWear.Application.Service;

public class NotificationService : INotificationService
{
    private readonly Supabase.Client _client;

    public NotificationService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
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
                FetchLowStockNotifications(notifications));

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
        var lowStock = await _client
            .From<ProductSizeStock>()
            .Filter("quantity",
                Supabase.Postgrest.Constants.Operator.LessThan,
                "5")
            .Filter("quantity",
                Supabase.Postgrest.Constants.Operator.GreaterThan,
                "0")
            .Limit(5)
            .Get();

        foreach (var stock in lowStock.Models)
        {
            notifications.Add(new NotificationDTO
            {
                Id = stock.Id,
                Type = "low_stock",
                Message = $"Low stock alert: Size {stock.Size} " +
                            $"(only {stock.Quantity} left)",
                CreatedAt = DateTime.UtcNow
            });
        }
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
                FetchLowStockNotifications(notifications));

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
}
