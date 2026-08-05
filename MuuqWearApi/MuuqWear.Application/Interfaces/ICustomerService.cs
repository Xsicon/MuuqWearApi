using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.CustomerDTO;

namespace MuuqWear.Application.Interfaces;

public interface ICustomerService
{
    Task<Response<PaginatedResponse<CustomerDTO>>> GetAll(
        string? search, int page, int pageSize, string? status = null);

    Task<Response<CustomerDTO>> GetById(Guid customerId);

    Task<Response<CustomerDTO>> Suspend(
        Guid customerId, SuspendCustomerDTO request, Guid adminUserId);

    Task<Response<CustomerDTO>> Reactivate(
        Guid customerId, ReactivateCustomerDTO? request, Guid adminUserId);

    Task<Response<List<CustomerNoteDTO>>> GetNotes(Guid customerId);

    Task<Response<CustomerNoteDTO>> CreateNote(
        Guid customerId, string body, Guid authorUserId);
}
