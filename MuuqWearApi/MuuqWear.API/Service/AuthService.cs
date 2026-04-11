using MuuqWear.API.DTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using Supabase;
using Supabase.Gotrue;
using Client = Supabase.Client;


namespace MuuqWear.API.Service;
public class AuthService : IAuthService
{
    private readonly Client _client;

    public AuthService(Client client)
    {
        _client = client;
    }

    public async Task<Response<int>> Register(RegisterRequestDTO request)
    {
        try
        {
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object>
    {
        { "full_name", request.FullName! }
    }
            };

            var result = await _client.Auth.SignUp(request.Email!, request.Password!, options);

            if (result?.User == null)
                return Response<int>.Fail("Registration failed");

            return Response<int>.SuccessResponse(1, "Registration successful");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            {
                return Response<int>.Fail("Email already exists");
            }


            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AuthResponseDTO>> VerifyOTP(VerifyOTPRequestDTO request)
    {
        try
        {
            var session = await _client.Auth.VerifyOTP(
                request.Email!,
                request.Otp!,
                Constants.EmailOtpType.Signup
            );

            if (session == null)
                return Response<AuthResponseDTO>.Fail("Verification failed");

            var authData = new AuthResponseDTO
            {
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken!,
                Email = session.User?.Email ?? "",
                UserId = session.User?.Id ?? ""
            };

            return Response<AuthResponseDTO>.SuccessResponse(
                authData,
                "Email Verified Successfully"
            );
        }
        catch (Exception ex)
        {
            return Response<AuthResponseDTO>.Fail("Error: " + ex.Message);
        }
    }
}
