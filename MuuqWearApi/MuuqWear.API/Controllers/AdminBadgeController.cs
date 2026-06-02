using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Service;
using MuuqWear.Model.DTO.AdminBadgeCount;

namespace MuuqWear.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin")]
public class AdminBadgeController : BaseController
{
    private readonly IAdminBadgeService _adminBadgeService;

    public AdminBadgeController(IAdminBadgeService adminBadgeService)
    {
        _adminBadgeService = adminBadgeService;
    }

    [HttpGet("counts")]
    public async Task<ActionResult<Response<AdminBadgeCountsDTO>>> GetCounts()
    {
        var response = await _adminBadgeService.GetCounts();
        if (!response.Success)
            return BadRequest(response);
        return HandleResponse(response);
    }
}
