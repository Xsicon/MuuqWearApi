using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.CustomerDTO;

namespace MuuqWear.Application.Interfaces;

public interface ICustomerService
{
    Task<Response<PaginatedResponse<CustomerDTO>>> GetAll(
        string? search, int page, int pageSize);
}
