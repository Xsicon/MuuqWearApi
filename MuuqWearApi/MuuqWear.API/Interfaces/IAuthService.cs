using MuuqWear.API.DTO;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Interfaces;
public interface IAuthService
{
    Task<Response<int>> Register(RegisterRequestDTO request);
    Task<Response<int>> SendMagicLink(string email);
    Task<Response<AuthResponseDTO>> VerifyOTP(VerifyOTPRequestDTO request);
    Task<Response<AuthResponseDTO>> Login(LoginRequestDTO request);
    Task<Response<int>> Logout();
    Task<Response<AuthResponseDTO>> VerifyMagicLink(string accessToken, string refreshToken);
    Task<Response<string>> GetGoogleSignInUrl();
    Task<Response<int>> SendPasswordReset(string email);
    Task<Response<int>> UpdatePassword(string accessToken, string refreshToken, string newPassword);

}

