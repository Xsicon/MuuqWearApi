using MuuqWear.API.DTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
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
                UserId = session.User?.Id ?? "",
                UserName = session.User?.UserMetadata["full_name"]?.ToString() ?? ""
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

    public async Task<Response<AuthResponseDTO>> Login(LoginRequestDTO request)
    {
        try
        {
            var session = await _client.Auth.SignIn(request.Email!, request.Password!);

            if (session?.User == null)
                return Response<AuthResponseDTO>.Fail("Invalid email or password");

            // fetch role from Profile table
            var userId = Guid.Parse(session.User.Id!);


            // Fetch profile safely
            var response = await _client
                .From<Profiles>()
                .Where(p => p.Id == userId)
                .Get();

            var profile = response.Models.FirstOrDefault();

            var authData = new AuthResponseDTO
            {
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken!,
                Email = session.User.Email ?? "",
                UserId = session.User.Id ?? "",
                UserName =profile?.FullName ?? "",
                Role = profile?.Role ?? "User"
            };

            return Response<AuthResponseDTO>.SuccessResponse(authData, "Login Successful");
        }
        catch (Exception ex)
        {
            return Response<AuthResponseDTO>.Fail("Error: " + ex.Message);
        }
    }
}
