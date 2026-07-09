using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.MuuqsimoDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MuuqsimoController : BaseController
{
    private readonly IMuuqsimoService _muuqsimoService;

    public MuuqsimoController(IMuuqsimoService muuqsimoService)
    {
        _muuqsimoService = muuqsimoService;
    }

    /// <summary>
    /// Public Muuqsimo event page payload (content JSON + live ticket products).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<Response<MuuqsimoPageDTO>>> Get([FromQuery] string? slug = null)
    {
        var result = await _muuqsimoService.GetPublishedPageAsync(slug);

        if (!result.Success)
        {
            var isMissing = result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                            || result.Message?.Contains("empty", StringComparison.OrdinalIgnoreCase) == true;
            return isMissing ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Published Muuqsimo events for storefront switcher (slug, title, dates).
    /// </summary>
    [HttpGet("events")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<List<MuuqsimoEventSummaryDTO>>>> ListPublished()
    {
        var result = await _muuqsimoService.GetPublishedEventsAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
