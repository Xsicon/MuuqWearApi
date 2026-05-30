using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MuuqWear.API.DTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Model.Models.Profiles;
using Supabase.Gotrue;
using System.Net.Http.Json;
using System.Text.Json;
using Client = Supabase.Client;


namespace MuuqWear.API.Service;
public class AuthService : IAuthService
{
    private readonly Supabase.Client _client;
    private readonly IConfiguration _configuration;

    public AuthService(SupabaseClientFactory factory, IConfiguration configuration)
    {
        _client = factory.CreateClient();
        _configuration = configuration;
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
                AffiliateCode=null,
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
     .Filter("id",
         Supabase.Postgrest.Constants.Operator.Equals,
         userId.ToString())
     .Get();

            var profile = response.Models.FirstOrDefault();
            if (profile?.IsDeleted == true)
                return Response<AuthResponseDTO>.Fail(
                    "This account has been deleted. Please contact support.");

            var authData = new AuthResponseDTO
            {
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken!,
                Email = session.User.Email ?? "",
                UserId = session.User.Id ?? "",
                UserName = profile?.FullName ?? "",
                Role = profile?.Role ?? "user"
            };

            return Response<AuthResponseDTO>.SuccessResponse(authData, "Login Successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            if (ex.Message.Contains("invalid_credentials"))
            {
                return Response<AuthResponseDTO>.Fail(
                    "Invalid email or password."
                );
            }

            return Response<AuthResponseDTO>.Fail(
                "Something went wrong. Please try again later."
            );
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

    public async Task<Response<int>> SendMagicLink(string email)
    {
        try
        {
            // validate email before calling Supabase
            // prevents unnecessary API calls 
            if (string.IsNullOrWhiteSpace(email))
                return Response<int>.Fail("Email is required");

            // Supabase sends magic link email
            // user clicks link → redirected to callback URL
            // configured in config.toml 
            var redirectUrl = _configuration["Auth:MagicLinkRedirectUrl"]
               ?? "http://localhost:5276/auth/magic-link-callback";

            await _client.Auth.SendMagicLink(
                email,
                new Supabase.Gotrue.SignInOptions
                {
                    RedirectTo = redirectUrl
                }
            );

            return Response<int>.SuccessResponse(1,
                "Magic link sent. Please check your email.");
        }
        catch (Exception ex)
        {
            // handle specific Supabase errors
            if (ex.Message.Contains("rate limit",
                StringComparison.OrdinalIgnoreCase))
                return Response<int>.Fail(
                    "Too many requests. Please wait before trying again.");

            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AuthResponseDTO>> VerifyMagicLink(
    string accessToken, string refreshToken)
    {
        try
        {
            // set session with tokens from magic link
            var session = await _client.Auth.SetSession(
                accessToken, refreshToken);

            if (session?.User == null)
                return Response<AuthResponseDTO>.Fail("Invalid magic link");

            // fetch role from profile table
            var profile = await _client
                .From<Profiles>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    session.User.Id!)
                .Single();

            //Insert in Profile
            if (profile == null)
            {
                profile = new Profiles
                {
                    Id = Guid.Parse(session.User.Id!),
                    FullName = session.User.UserMetadata
                        .ContainsKey("full_name")
                            ? session.User.UserMetadata["full_name"]?.ToString() ?? ""
                            : "",
                    Role = "user",
                    Email = session.User.Email,
                    CreatedAt = DateTime.UtcNow
                };

                await _client.From<Profiles>().Insert(profile);
            }

            var authData = new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = session.User.Email ?? "",
                UserId = session.User.Id ?? "",
                UserName = session.User.UserMetadata
                    .ContainsKey("full_name")
                        ? session.User.UserMetadata["full_name"]?.ToString() ?? ""
                        : "",
                Role = profile?.Role ?? "user"
            };

            return Response<AuthResponseDTO>.SuccessResponse(
                authData, "Magic link verified successfully");
        }
        catch (Exception ex)
        {
            return Response<AuthResponseDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<string>> GetGoogleSignInUrl()
    {
        try
        {
            // SignIn returns ProviderAuthState which contains the URL
            var state = await _client.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                new Supabase.Gotrue.SignInOptions
                {
                    RedirectTo = _configuration["Auth:GoogleCallbackUrl"]
                        ?? "http://localhost:5276/auth/google-callback"
                });

            // Uri property contains the Google OAuth URL 
            var url = state?.Uri?.ToString();

            if (string.IsNullOrEmpty(url))
                return Response<string>.Fail("Failed to get Google sign in URL");

            return Response<string>.SuccessResponse(url, "Google sign in URL generated");
        }
        catch (Exception ex)
        {
            return Response<string>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<int>> SendPasswordReset(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return Response<int>.Fail("Email is required");

            var redirectUrl = _configuration["Auth:PasswordResetCallbackUrl"]
                ?? "http://localhost:5276/auth/reset-password";

            // correct syntax for Supabase C# client 1.1.1
            await _client.Auth.ResetPasswordForEmail(email);

            return Response<int>.SuccessResponse(1,
                "Password reset email sent. Please check your email.");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("rate limit",
                StringComparison.OrdinalIgnoreCase))
                return Response<int>.Fail(
                    "Too many requests. Please wait before trying again.");

            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<int>> UpdatePassword(
    string accessToken,
    string refreshToken,
    string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return Response<int>.Fail("Invalid token");

            if (string.IsNullOrWhiteSpace(newPassword))
                return Response<int>.Fail("Password is required");

            if (newPassword.Length < 8)
                return Response<int>.Fail("Password must be at least 8 characters");

            // set session using recovery token
            var session = await _client.Auth.SetSession(
                accessToken, refreshToken);

            if (session?.User == null)
                return Response<int>.Fail("Invalid or expired reset link");

            // update password using Supabase
            await _client.Auth.Update(new Supabase.Gotrue.UserAttributes
            {
                Password = newPassword
            });

            return Response<int>.SuccessResponse(1,
                "Password updated successfully");
        }
        catch (Exception ex)
        {
            // token expired
            if (ex.Message.Contains("expired",
                StringComparison.OrdinalIgnoreCase))
                return Response<int>.Fail(
                    "Reset link has expired. Please request a new one.");

            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<AuthResponseDTO>> RefreshToken(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Response<AuthResponseDTO>.Fail("Refresh token is required");

            var supabaseUrl = _configuration["SupaBase:Url"];
            var supabaseKey = _configuration["SupaBase:ServiceRoleKey"];

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("apikey", supabaseKey);

            var body = new { refresh_token = refreshToken };
            var result = await http.PostAsJsonAsync(
                $"{supabaseUrl}/auth/v1/token?grant_type=refresh_token", body);

            if (!result.IsSuccessStatusCode)
            {
                var errorBody = await result.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Supabase rejected refresh: {errorBody}"); // ← add this

                return Response<AuthResponseDTO>.Fail("Invalid or expired refresh token");
            }
            var json = await result.Content.ReadFromJsonAsync<JsonElement>();
            var userId = json.GetProperty("user")
                        .GetProperty("id").GetString() ?? "";

            //  check is_deleted after successful token refresh
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                var profileResponse = await _client
                    .From<Profiles>()
                    .Where(p => p.Id == userGuid)
                    .Get();

                var profile = profileResponse.Models.FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"RefreshToken check — userId: {userId}");
                System.Diagnostics.Debug.WriteLine($"Profile found: {profile != null}");
                System.Diagnostics.Debug.WriteLine($"IsDeleted: {profile?.IsDeleted}");
                if (profile?.IsDeleted == true)
                    return Response<AuthResponseDTO>.Fail(
                        "This account has been deleted.");
            }
            var authData = new AuthResponseDTO
            {
                AccessToken = json.GetProperty("access_token").GetString()!,
                RefreshToken = json.GetProperty("refresh_token").GetString()!,
                Email = json.GetProperty("user").GetProperty("email").GetString() ?? "",
                UserId = json.GetProperty("user").GetProperty("id").GetString() ?? "",
            };

            return Response<AuthResponseDTO>.SuccessResponse(
                authData, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            return Response<AuthResponseDTO>.Fail("Error: " + ex.Message);
        }
    }
}
