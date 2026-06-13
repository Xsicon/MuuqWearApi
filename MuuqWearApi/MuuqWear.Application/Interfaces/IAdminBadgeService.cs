
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.AdminBadgeCount;

namespace MuuqWear.Application.Interfaces;
public interface IAdminBadgeService
{
    Task<Response<AdminBadgeCountsDTO>> GetCounts();

}
