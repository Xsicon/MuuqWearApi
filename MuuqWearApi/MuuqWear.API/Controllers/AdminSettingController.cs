using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Service;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")] //  all endpoints admin only
public class AdminSettingController : BaseController
{
    private readonly IAdminSettingService _adminSettingService;

    public AdminSettingController(IAdminSettingService adminSettingService)
    {
        _adminSettingService = adminSettingService;
    }

    // =============================================
    // GET ALL ADMIN USERS
    // GET api/AdminUser
    // =============================================
    [HttpGet]
    public async Task<ActionResult<Response<List<AdminSettingsUserDTO>>>> GetAll()
    {
        var response = await _adminSettingService.GetAll();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // INVITE ADMIN USER
    // POST api/AdminUser/invite
    // =============================================
    [HttpPost("invite")]
    public async Task<ActionResult<Response<AdminSettingsUserDTO>>> Invite(
        [FromBody] InviteAdminSettingsUserDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Email is required"));

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Role is required"));

        var response = await _adminSettingService.Invite(request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE ADMIN USER
    // PATCH api/AdminUser/{userId}
    // =============================================
    [HttpPatch("{userId}")]
    public async Task<ActionResult<Response<AdminSettingsUserDTO>>> Update(
        Guid userId,
        [FromBody] UpdateAdminSettingsUserDTO request)
    {
        if (userId == Guid.Empty)
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Invalid user id"));

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(Response<AdminSettingsUserDTO>
                .Fail("Role is required"));

        var response = await _adminSettingService.Update(userId, request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // DEACTIVATE ADMIN USER
    // DELETE api/AdminUser/{userId}
    // =============================================
    [HttpDelete("{userId}")]
    public async Task<ActionResult<Response<bool>>> Deactivate(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(Response<bool>
                .Fail("Invalid user id"));

        var response = await _adminSettingService.Deactivate(userId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    [HttpGet("supabase-health")]
    public async Task<ActionResult<Response<SupabaseHealthDTO>>> CheckSupabaseHealth()
    {
        var response = await _adminSettingService.CheckSupabaseHealth();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    [HttpGet("stripe-health")]
    public async Task<ActionResult<Response<StripeHealthDTO>>> CheckStripeHealth()
    {
        var response = await _adminSettingService.CheckStripeHealth();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
