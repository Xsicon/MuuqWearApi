using MuuqWear.API.DTO;
using MuuqWear.API.Shared;
namespace MuuqWear.Application.Interfaces;
public interface IOrderService
{
    Task<Response<OrderDTO>> PlaceOrder(Guid userId, PlaceOrderDTO request);
    Task<Response<OrderDTO>> GetOrder(Guid orderId, Guid userId);
    Task<Response<List<OrderDTO>>> GetUserOrders(Guid userId);
}
