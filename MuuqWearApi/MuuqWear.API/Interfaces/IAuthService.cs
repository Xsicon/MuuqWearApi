using MuuqWear.API.DTO;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Interfaces;
public interface IAuthService
{
    Task<Response<int>> Register(RegisterRequestDTO request);
    Task<Response<AuthResponseDTO>> VerifyOTP(VerifyOTPRequestDTO request);
}

