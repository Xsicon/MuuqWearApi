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
        Supabase.Gotrue.User? user = null;
        try
        {
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object>
    {
        { "full_name", request.FullName! }
    }
            };

            var response = await _client.Auth.SignUp(request.Email!, request.Password!, options);
            user = response?.User;

            if (user == null)
                return Response<int>.Fail("Registration failed");

            await _client.From<Profiles>().Insert(new Profiles
            {
                Id = Guid.Parse(user.Id!),
                FullName = request?.FullName,
                Phone = null,
                Role = "user",
                Email = request?.Email,
                CreatedAt = DateTime.UtcNow
            });

            return Response<int>.SuccessResponse(1, "Registered successfully. Please verify your email.");
        }
        catch (Exception ex)
        {
            // if profile insert failed after signup
            if (ex.Message.Contains("23505"))
                return Response<int>.Fail("Email already registered. Please login.");

            if (ex.Message.Contains("23502"))
                return Response<int>.Fail("Registration failed. Please try again.");

            return Response<int>.Fail("Something went wrong. Please try again.");
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
            var fullName = "";
            if (session.User?.UserMetadata != null && session.User.UserMetadata.ContainsKey("full_name"))
            {
                fullName = session.User.UserMetadata["full_name"]?.ToString() ?? "";
            }
            var profile = await _client
    .From<Profiles>()
    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, session.User!.Id!)
    .Single();
            var authData = new AuthResponseDTO
            {
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken!,
                Email = session.User?.Email ?? "",
                UserId = session.User?.Id ?? "",
                UserName = fullName,
                Role = profile?.Role!
            };

            // fetch role separately — don't let it break OTP flow

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
                UserName = profile?.FullName ?? "",
                Role = profile?.Role ?? "User"
            };

            return Response<AuthResponseDTO>.SuccessResponse(authData, "Login Successful");
        }
        catch (Exception ex)
        {
            return Response<AuthResponseDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<int>> Logout()
    {
        try
        {
            await _client.Auth.SignOut();
            return Response<int>.SuccessResponse(1, "Logged out successfully");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail("Error: " + ex.Message);
        }
    }
}
