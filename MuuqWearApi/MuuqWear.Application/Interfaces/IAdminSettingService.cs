using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;

namespace MuuqWear.Application.Interfaces;

public interface IAdminSettingService
{
    Task<Response<List<AdminSettingsUserDTO>>> GetAll();
    Task<Response<AdminSettingsUserDTO>> Invite(InviteAdminSettingsUserDTO request);
    Task<Response<AdminSettingsUserDTO>> Update(Guid userId, UpdateAdminSettingsUserDTO request);
    Task<Response<bool>> Deactivate(Guid userId);
    Task<Response<SupabaseHealthDTO>> CheckSupabaseHealth();
    Task<Response<StripeHealthDTO>> CheckStripeHealth();

}
