using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.RefundDTO;
using MuuqWear.Model.Models.Order;

namespace MuuqWear.Application.Interfaces;

public interface IRefundService
{
    Task<Response<PaginatedResponse<RefundDTO>>> GetAllRefunds(
        string? status, int page, int pageSize);

    Task<Response<RefundDTO>> GetRefundById(Guid refundId);

    Task<Response<RefundDTO>> ProcessRefund(Guid refundId);

    Task CreatePendingRefundFromReturn(OrderReturn orderReturn);
}
