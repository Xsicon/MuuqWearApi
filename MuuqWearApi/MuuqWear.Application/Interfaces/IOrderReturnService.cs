using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.OrdeReturnDTO;

namespace MuuqWear.Application.Interfaces;

public interface IOrderReturnService
{
    Task<Response<OrderReturnDTO>> SubmitReturn(SubmitReturnDTO request);
    Task<Response<PaginatedResponse<OrderReturnDTO>>> GetAllReturns(
        string? status, int page, int pageSize);
    Task<Response<OrderReturnDTO>> UpdateReturnStatus(
        Guid returnId, string status);
}
