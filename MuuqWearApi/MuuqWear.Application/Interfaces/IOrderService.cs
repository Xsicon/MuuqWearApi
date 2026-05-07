using MuuqWear.API.DTO;
using MuuqWear.API.Shared;
namespace MuuqWear.Application.Interfaces;
public interface IOrderService
{
    Task<Response<OrderDTO>> PlaceOrder(Guid userId, PlaceOrderDTO request);
    Task<Response<OrderDTO>> GetOrder(Guid orderId, Guid userId);
    Task<Response<List<OrderDTO>>> GetUserOrders(Guid userId);

    // ← new admin methods
    Task<Response<PaginatedResponse<OrderDTO>>> GetAllOrders(
        string? status, string? search, int page, int pageSize);
    Task<Response<OrderDTO>> GetOrderDetail(Guid orderId);
    Task<Response<OrderDTO>> UpdateOrderStatus(Guid orderId, string status);
    Task<Response<int>> BulkUpdateOrderStatus(List<Guid> orderIds, string status);
}
