using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.ProfileDTO;

namespace MuuqWear.Application.Interfaces;

public interface IProfileService
{
    Task<Response<ProfileDTO>> GetProfile(Guid userId);
    Task<Response<ProfileDTO>> UpdateProfile(Guid userId, UpdateProfileDTO request);
    Task<Response<bool>> DeleteAccount(Guid userId);
}

