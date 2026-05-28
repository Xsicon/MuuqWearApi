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

    // ─── GET ALL PUBLISHED ────────────────────────────────────

    [HttpGet]
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

    // ─── GET SINGLE PUBLISHED ─────────────────────────────────
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetById(Guid id)
    {
        var result = await _contentService.GetById(
            ContentCategory.JournalArticles, id);

        if (!result.Success) return BadRequest(result);

        //  only return if published
        if (result.Data?.Status != "published")
            return NotFound(Response<ContentItemDTO>.Fail("Article not found"));

        return Ok(result);
    }
}
