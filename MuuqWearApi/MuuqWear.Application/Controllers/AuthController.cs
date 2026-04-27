using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO;
using MuuqWear.API.DTO.AuthDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<Response<int>>> Register(RegisterRequestDTO request)
        {
            var response = await _authService.Register(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("verifyotp")]
        public async Task<ActionResult<Response<AuthResponseDTO>>> VerifyOTP(VerifyOTPRequestDTO request)
        {
            var response = await _authService.VerifyOTP(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<Response<AuthResponseDTO>>> Login(LoginRequestDTO request)
        {
            var response = await _authService.Login(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<ActionResult<Response<int>>> Logout()
        {
            var response = await _authService.Logout();
            Response.Cookies.Delete("muuqwear_auth");

            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("magic-link")]
        public async Task<ActionResult<Response<int>>> SendMagicLink(
    [FromBody] MagicLinkRequestDTO request)
        {
            // validate request
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(Response<int>.Fail("Email is required"));

            var response = await _authService.SendMagicLink(request.Email);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("verify-magic-link")]
        public async Task<ActionResult<Response<AuthResponseDTO>>> VerifyMagicLink(
    [FromBody] MagicLinkVerifyRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.AccessToken))
                return BadRequest(Response<AuthResponseDTO>.Fail("Access token required"));

            var response = await _authService.VerifyMagicLink(
                request.AccessToken, request.RefreshToken ?? "");

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("google-signin-url")]
        public async Task<ActionResult<Response<string>>> GetGoogleSignInUrl()
        {
            var response = await _authService.GetGoogleSignInUrl();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<Response<int>>> ForgotPassword(
    [FromBody] ForgotPasswordRequestDTO request)
        {
            // validate request
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(Response<int>.Fail("Email is required"));

            var response = await _authService.SendPasswordReset(request.Email);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<Response<int>>> ResetPassword(
            [FromBody] ResetPasswordRequestDTO request)
        {
            // validate request
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return BadRequest(Response<int>.Fail("Invalid token"));

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(Response<int>.Fail("Password is required"));

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(Response<int>.Fail("Passwords do not match"));

            var response = await _authService.UpdatePassword(
                request.AccessToken,
                request.RefreshToken ?? "",
                request.NewPassword);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

    }

}
