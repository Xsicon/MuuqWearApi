using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.ProfileDTO;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MuuqWear.Application.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.GetProfile(userId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<ProfileDTO>.Fail("Full name is required"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.UpdateProfile(userId, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.DeleteAccount(userId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── CHECK IF ACTIVE ──────────────────────────────────────────
    [HttpGet("is-active")]
    public async Task<IActionResult> IsActive()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.GetProfile(userId);

        if (!result.Success)
            return BadRequest(Response<bool>.Fail("Profile not found"));

        // ✅ return whether account is active
        if (result.Data?.IsDeleted == true)
            return Ok(Response<bool>.SuccessResponse(false, "Account deleted"));

        return Ok(Response<bool>.SuccessResponse(true, "Account active"));
    }
}