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
public class ProfileController : BaseController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }


    [HttpGet]
    public async Task<ActionResult<Response<ProfileDTO>>> Get()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<ProfileDTO>.Fail("Not authenticated"));

        var result = await _profileService.GetProfile(userId);
        return HandleResponse(result);
    }

    [HttpPut]
    public async Task<ActionResult<Response<ProfileDTO>>> Update([FromBody] UpdateProfileDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<ProfileDTO>.Fail("Full name is required"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.UpdateProfile(userId, request);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
    }

    [HttpDelete("delete-account")]
    public async Task<ActionResult<Response<bool>>> DeleteAccount()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        var result = await _profileService.DeleteAccount(userId);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
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

        //  return whether account is active
        if (result.Data?.IsDeleted == true)
            return Ok(Response<bool>.SuccessResponse(false, "Account deleted"));

        return Ok(Response<bool>.SuccessResponse(true, "Account active"));
    }

    // =============================================
    // UPDATE LAST ACTIVE
    // POST api/Profile/last-active/{userId}
    // internal only — called by middleware
    // =============================================
    [HttpPost("last-active/{userId}")]
    [AllowAnonymous] // ← middleware has no JWT to send
    public async Task<IActionResult> UpdateLastActive(Guid userId)
    {
        if (userId == Guid.Empty) return Ok(); // ← silent fail

        try
        {
            await _profileService.UpdateLastActive(userId);
        }
        catch
        {
            //  silent fail — never crash page load
        }

        return Ok();
    }
}