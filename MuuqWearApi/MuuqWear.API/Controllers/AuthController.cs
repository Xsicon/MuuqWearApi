using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO;
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

        [HttpGet("me")]
        public IActionResult Me()
        {
            var authCookie = Request.Cookies["muuqwear_auth"];
            System.Diagnostics.Debug.WriteLine($"Cookie value: " +
                $"{authCookie?.Substring(0, Math.Min(50, authCookie?.Length ?? 0))}");

            if (string.IsNullOrEmpty(authCookie))
                return Unauthorized();

            try
            {
                var authData = System.Text.Json.JsonSerializer.Deserialize<AuthResponseDTO>(authCookie);

                if (authData == null)
                    return Unauthorized();

                return Ok(Response<AuthResponseDTO>.SuccessResponse(authData, "User found"));
            }
            catch
            {
                return Unauthorized();
            }
        }

    }
}
