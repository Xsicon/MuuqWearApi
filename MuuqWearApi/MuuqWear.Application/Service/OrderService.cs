using MuuqWear.API.DTO;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.Models;
using Supabase;

namespace MuuqWear.Application.Service;
public class OrderService : IOrderService
{
    private readonly Supabase.Client _client;

    public OrderService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // PLACE ORDER
    // =============================================
    public async Task<Response<OrderDTO>> PlaceOrder(
        Guid userId, PlaceOrderDTO request)
    {
        try
        {
            // Step 1 — validate email 
            if (string.IsNullOrWhiteSpace(request.Email))
                return Response<OrderDTO>.Fail("Email is required");

            // Step 2 — fetch user cart items 
            var cartItems = await _client
                .From<CartItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            if (!cartItems.Models.Any())
                return Response<OrderDTO>.Fail("Cart is empty");

            // Step 3 — fetch product details for each cart item 
            // needed for snapshot + price calculation
            var orderItems = new List<OrderItem>();
            decimal subtotal = 0;

            foreach (var cartItem in cartItems.Models)
            {
                var product = await _client
                    .From<Product>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        cartItem.ProductId.ToString())
                    .Single();

                if (product == null) continue;

                var itemTotal = product.Price * cartItem.Quantity;
                subtotal += itemTotal;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = cartItem.ProductId,
                    // snapshot 
                    ProductName = product.Name ?? "",
                    ProductImageUrl = product.ImageUrl,
                    Size = cartItem.Size,
                    Quantity = cartItem.Quantity,
                    Price = product.Price,
                    ItemTotal = itemTotal,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Step 4 — calculate totals 
            var shipping = 0m;
            var tax = Math.Round(subtotal * 0.10m, 2);
            var total = subtotal + shipping + tax;

            // Step 5 — generate order number 
            // format: MQ-XXXXXXXX
            var orderNumber = $"MQ-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

            // Step 6 — create order 
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = userId,
                Email = request.Email,
                Subtotal = subtotal,
                Shipping = shipping,
                Tax = tax,
                Total = total,
                Status = "pending",
                FirstName = request.FirstName,
                LastName = request.LastName,
                Address = request.Address,
                City = request.City,
                PostalCode = request.PostalCode,
                CreatedAt = DateTime.UtcNow
            };

            var orderResult = await _client
                .From<Order>()
                .Insert(order);

            var insertedOrder = orderResult.Models.FirstOrDefault();
            if (insertedOrder == null)
                return Response<OrderDTO>.Fail("Failed to create order");

            // Step 7 — insert order items 
            foreach (var item in orderItems)
            {
                item.OrderId = insertedOrder.Id;
                await _client.From<OrderItem>().Insert(item);
            }

            // Step 8 — clear cart after order 
            await _client
                .From<CartItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Delete();

            // Step 9 — return order 
            var orderDTO = new OrderDTO
            {
                Id = insertedOrder.Id,
                OrderNumber = insertedOrder.OrderNumber,
                Email = insertedOrder.Email,
                Subtotal = insertedOrder.Subtotal,
                Shipping = insertedOrder.Shipping,
                Tax = insertedOrder.Tax,
                Total = insertedOrder.Total,
                Status = insertedOrder.Status,
                CreatedAt = insertedOrder.CreatedAt,
                Items = orderItems.Select(i => new OrderItemDTO
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    ItemTotal = i.ItemTotal
                }).ToList()
            };

            return Response<OrderDTO>.SuccessResponse(
                orderDTO, "Order placed successfully");
        }
        catch (Exception ex)
        {
            return Response<OrderDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ORDER
    // =============================================
    public async Task<Response<OrderDTO>> GetOrder(
        Guid orderId, Guid userId)
    {
        try
        {
            // fetch order 
            var order = await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderId.ToString())
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Single();

            if (order == null)
                return Response<OrderDTO>.Fail("Order not found");

            // fetch order items 
            var items = await _client
                .From<OrderItem>()
                .Filter("order_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderId.ToString())
                .Get();

            var orderDTO = new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Email = order.Email,
                Subtotal = order.Subtotal,
                Shipping = order.Shipping,
                Tax = order.Tax,
                Total = order.Total,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                Items = items.Models.Select(i => new OrderItemDTO
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    ItemTotal = i.ItemTotal
                }).ToList()
            };

            return Response<OrderDTO>.SuccessResponse(
                orderDTO, "Order fetched");
        }
        catch (Exception ex)
        {
            return Response<OrderDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET USER ORDERS
    // =============================================
    public async Task<Response<List<OrderDTO>>> GetUserOrders(Guid userId)
    {
        try
        {
            var orders = await _client
                .From<Order>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var orderDTOs = orders.Models.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Email = o.Email,
                Subtotal = o.Subtotal,
                Shipping = o.Shipping,
                Tax = o.Tax,
                Total = o.Total,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            }).ToList();

            return Response<List<OrderDTO>>.SuccessResponse(
                orderDTOs, "Orders fetched");
        }
        catch (Exception ex)
        {
            return Response<List<OrderDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ALL ORDERS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<OrderDTO>>> GetAllOrders(
        string? status, string? search, int page, int pageSize)
    {
        try
        {
            var statusParam = status?.Trim() ?? "";
            var searchParam = search?.Trim() ?? "";
            var offset = (page - 1) * pageSize;

            //  count first
            var countResult = await _client.Rpc(
                "get_orders_count",
                new Dictionary<string, object>
                {
                { "p_status",      statusParam },
                { "p_search_term", searchParam }
                });

            var totalCount = 0;
            int.TryParse(countResult.Content?.Trim('"'), out totalCount);

            //  fetch paginated data
            var dataResult = await _client.Rpc(
                "get_orders",
                new Dictionary<string, object>
                {
                { "p_status",      statusParam },
                { "p_search_term", searchParam },
                { "p_page_size",   pageSize    },
                { "p_offset",      offset      }
                });

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            };

            var orders = System.Text.Json.JsonSerializer
                .Deserialize<List<OrderDTO>>(
                    dataResult.Content ?? "[]", options)
                ?? new List<OrderDTO>();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var paginated = new PaginatedResponse<OrderDTO>
            {
                Data = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<OrderDTO>>
                .SuccessResponse(paginated, "Orders fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<OrderDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ORDER DETAIL (ADMIN) — slide-in panel
    // =============================================
    public async Task<Response<OrderDTO>> GetOrderDetail(Guid orderId)
    {
        try
        {
            //  fetch order
            var order = await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderId.ToString())
                .Single();

            if (order == null)
                return Response<OrderDTO>.Fail("Order not found");

            //  fetch items
            var itemsResult = await _client
                .From<OrderItem>()
                .Filter("order_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderId.ToString())
                .Get();

            var orderDTO = new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Email = order.Email,
                Subtotal = order.Subtotal,
                Shipping = order.Shipping,
                Tax = order.Tax,
                Total = order.Total,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                FirstName = order.FirstName,
                LastName = order.LastName,
                Address = order.Address,
                City = order.City,
                PostalCode = order.PostalCode,
                Items = itemsResult.Models.Select(i => new OrderItemDTO
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    ItemTotal = i.ItemTotal
                }).ToList()
            };

            return Response<OrderDTO>.SuccessResponse(orderDTO, "Order fetched");
        }
        catch (Exception ex)
        {
            return Response<OrderDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE ORDER STATUS (ADMIN)
    // =============================================
    public async Task<Response<OrderDTO>> UpdateOrderStatus(
        Guid orderId, string status)
    {
        try
        {
            //  validate status — only known values accepted
            var validStatuses = new[]
            {
            OrderStatus.Pending,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled
        };

            if (!validStatuses.Contains(status))
                return Response<OrderDTO>.Fail("Invalid status");

            var result = await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    orderId.ToString())
                .Set(o => o.Status!, status)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<OrderDTO>.Fail("Order not found");

            return Response<OrderDTO>.SuccessResponse(new OrderDTO
            {
                Id = updated.Id,
                OrderNumber = updated.OrderNumber,
                Status = updated.Status,
                Total = updated.Total,
                Email = updated.Email,
                CreatedAt = updated.CreatedAt
            }, "Status updated");
        }
        catch (Exception ex)
        {
            return Response<OrderDTO>.Fail("Error: " + ex.Message);
        }
    }
    // =============================================
    // BULK UPDATE ORDER STATUS (ADMIN)
    // =============================================
    public async Task<Response<int>> BulkUpdateOrderStatus(
        List<Guid> orderIds, string status)
    {
        try
        {
            if (!orderIds.Any())
                return Response<int>.Fail("No orders selected");

            //  validate status
            var validStatuses = new[]
            {
            OrderStatus.Pending,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled
        };

            if (!validStatuses.Contains(status))
                return Response<int>.Fail("Invalid status");

            //  single DB update for all ids at once
            // convert ids to string array for Supabase IN filter
            var idStrings = orderIds
                .Select(id => id.ToString())
                .ToList();

            await _client
                .From<Order>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.In,
                    idStrings)
                .Set(o => o.Status!, status)
                .Update();

            return Response<int>.SuccessResponse(
                orderIds.Count,
                $"{orderIds.Count} orders updated to {status}");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail("Error: " + ex.Message);
        }
    }
}

