using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.ContentItemDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalController : BaseController
{
    private readonly IContentService _contentService;

    public JournalController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<Response<PaginatedResponse<ContentItemDTO>>>> GetPublished(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6,
        [FromQuery] string? category = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 6;

        var result = await _contentService.GetPublished(page, pageSize, category);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetFeatured()
    {
        var result = await _contentService.GetFeaturedPublished();
        if (!result.Success)
            return IsMissing(result.Message) ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetBySlug(string slug)
    {
        var result = await _contentService.GetPublishedBySlug(slug);
        if (!result.Success)
            return IsMissing(result.Message) ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetById(Guid id)
    {
        var result = await _contentService.GetById(
            ContentCategory.JournalArticles, id);

        if (!result.Success)
            return IsMissing(result.Message) ? NotFound(result) : BadRequest(result);

        if (result.Data?.Status != "published")
            return NotFound(Response<ContentItemDTO>.Fail("Article not found"));

        return Ok(result);
    }

    [HttpPost("{id:guid}/view")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<int>>> RecordView(Guid id)
    {
        var result = await _contentService.RecordJournalView(id);
        if (!result.Success)
            return IsMissing(result.Message) ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    private static bool IsMissing(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && (message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("0 rows", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no rows", StringComparison.OrdinalIgnoreCase));
}
