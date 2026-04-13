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
    }
}
