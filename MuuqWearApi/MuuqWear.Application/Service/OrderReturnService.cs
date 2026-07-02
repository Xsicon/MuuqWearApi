using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.OrdeReturnDTO;
using MuuqWear.Model.Models.Order;

namespace MuuqWear.Application.Service;

public class OrderReturnService : IOrderReturnService
{
    private readonly Supabase.Client _client;
    private readonly IRefundService _refundService;

    public OrderReturnService(
        SupabaseClientFactory factory,
        IRefundService refundService)
    {
        _client = factory.CreateClient();
        _refundService = refundService;
    }

    // =============================================
    // SUBMIT RETURN
    // =============================================
    public async Task<Response<OrderReturnDTO>> SubmitReturn(
        SubmitReturnDTO request)
    {
        try
        {
            // Step 1 — validate order number + email match
            var order = await _client
                .From<Order>()
                .Filter("order_number",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.OrderNumber.Trim())
                .Filter("email",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.Email.Trim().ToLower())
                .Single();

            if (order == null)
                return Response<OrderReturnDTO>.Fail(
                    "Order not found. Please check your order number and email.");
            if (order.Status?.ToLower() != "delivered")
                return Response<OrderReturnDTO>.Fail(
                    order.Status?.ToLower() switch
                    {
                        "pending" => "Your order has not shipped yet. To cancel, please contact support.",
                        "processing" => "Your order is being processed. To cancel, please contact support.",
                        "shipped" => "Your order is on its way. You can return it once delivered.",
                        "cancelled" => "This order has already been cancelled.",
                        _ => "Returns are only available for delivered orders."
                    });

            if (order.CreatedAt.HasValue)
            {
                var daysSinceOrder = (DateTime.UtcNow - order.CreatedAt.Value).TotalDays;
                if (daysSinceOrder > 30)
                    return Response<OrderReturnDTO>.Fail(
                        "Return window has expired. Returns are only accepted within 30 days of delivery.");
            }
            // Step 2 — check no duplicate active return for same order
            var existing = await _client
                .From<OrderReturn>()
                .Filter("order_id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                    order.Id.ToString())
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.NotEqual,
                    ReturnStatus.Denied)
                .Get();

            if (existing.Models.Any())
                return Response<OrderReturnDTO>.Fail(
                    "A return request already exists for this order.");

            // Step 3 — auto-generate return number
            var returnNumber = $"R-{order.Id.ToString().Substring(0, 6).ToUpper()}";

            // Step 4 — insert return
            var orderReturn = new OrderReturn
            {
                Id = Guid.NewGuid(),
                ReturnNumber = returnNumber,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Email = request.Email.Trim().ToLower(),
                FullName = request.FullName.Trim(),
                ItemsToReturn = request.ItemsToReturn.Trim(),
                Reason = request.Reason.Trim(),
                Comments = request.Comments?.Trim(),
                Status = ReturnStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _client
                .From<OrderReturn>()
                .Insert(orderReturn);

            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<OrderReturnDTO>.Fail("Failed to submit return");

            // Step 5 — return DTO
            var returnDTO = new OrderReturnDTO
            {
                Id = inserted.Id,
                ReturnNumber = inserted.ReturnNumber,
                OrderId = inserted.OrderId,
                OrderNumber = inserted.OrderNumber,
                Email = inserted.Email,
                FullName = inserted.FullName,
                ItemsToReturn = inserted.ItemsToReturn,
                Reason = inserted.Reason,
                Comments = inserted.Comments,
                Status = inserted.Status,
                CreatedAt = inserted.CreatedAt
            };

            return Response<OrderReturnDTO>.SuccessResponse(
                returnDTO, "Return submitted successfully");
        }
        catch (Exception ex)
        {
            return Response<OrderReturnDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ALL RETURNS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<OrderReturnDTO>>> GetAllReturns(
     string? status, int page, int pageSize)
    {
        try
        {
            var statusParam = status?.Trim() ?? "";
            var offset = (page - 1) * pageSize;

            // Step 1 — count via RPC
            var countResult = await _client.Rpc(
                "get_order_returns_count",
                new Dictionary<string, object>
                {
                { "p_status", statusParam }
                });

            var totalCount = 0;
            int.TryParse(countResult.Content?.Trim('"'), out totalCount);

            // Step 2 — fetch paginated data via RPC
            var dataResult = await _client.Rpc(
                "get_order_returns",
                new Dictionary<string, object>
                {
                { "p_status",    statusParam },
                { "p_page_size", pageSize    },
                { "p_offset",    offset      }
                });

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            };

            // Step 3 — deserialize
            var returns = System.Text.Json.JsonSerializer
                .Deserialize<List<OrderReturnDTO>>(
                    dataResult.Content ?? "[]", options)
                ?? new List<OrderReturnDTO>();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginated = new PaginatedResponse<OrderReturnDTO>
            {
                Data = returns,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<OrderReturnDTO>>
                .SuccessResponse(paginated, "Returns fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<OrderReturnDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE RETURN STATUS (ADMIN)
    // =============================================
    public async Task<Response<OrderReturnDTO>> UpdateReturnStatus(
        Guid returnId, string status)
    {
        try
        {
            // Step 1 — validate status
            var validStatuses = new[]
            {
                ReturnStatus.Pending,
                ReturnStatus.Approved,
                ReturnStatus.Denied
            };

            if (!validStatuses.Contains(status))
                return Response<OrderReturnDTO>.Fail("Invalid status");

            // Step 2 — update
            var result = await _client
                .From<OrderReturn>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    returnId.ToString())
                .Set(r => r.Status!, status)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<OrderReturnDTO>.Fail("Return not found");

            if (status.ToLower() == ReturnStatus.Approved.ToLower())
            {
                await RestoreStockForOrder(updated.OrderId.Value);
                await _refundService.CreatePendingRefundFromReturn(updated);
            }

            // Step 3 — return DTO inline
            return Response<OrderReturnDTO>.SuccessResponse(
                new OrderReturnDTO
                {
                    Id = updated.Id,
                    ReturnNumber = updated.ReturnNumber,
                    OrderId = updated.OrderId,
                    OrderNumber = updated.OrderNumber,
                    Email = updated.Email,
                    FullName = updated.FullName,
                    ItemsToReturn = updated.ItemsToReturn,
                    Reason = updated.Reason,
                    Comments = updated.Comments,
                    Status = updated.Status,
                    CreatedAt = updated.CreatedAt
                }, "Status updated");
        }
        catch (Exception ex)
        {
            return Response<OrderReturnDTO>.Fail("Error: " + ex.Message);
        }
    }

    private async Task RestoreStockForOrder(Guid orderId)
    {
        // fetch order items
        var orderItems = await _client
            .From<OrderItem>()
            .Filter("order_id",
                Supabase.Postgrest.Constants.Operator.Equals,
                orderId.ToString())
            .Get();

        // restore stock for each item
        foreach (var item in orderItems.Models)
        {
            await _client.Rpc(
                "restore_stock",
                new Dictionary<string, object>
                {
                { "p_product_id", item.ProductId.ToString() },
                { "p_size",       item.Size ?? "" },
                { "p_quantity",   item.Quantity }
                });
        }
    }
}
