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
    public async Task<ActionResult<Response<List<ContentItemDTO>>>> GetPublished()
    {
        var result = await _contentService.GetAll(ContentCategory.JournalArticles);
        if (!result.Success) return BadRequest(result);

        // ✅ filter published only — public endpoint
        var published = result.Data?
            .Where(x => x.Status == "published")
            .ToList() ?? new();

        return Ok(Response<List<ContentItemDTO>>.SuccessResponse(
            published, "Articles fetched"));
    }

    // ─── GET SINGLE PUBLISHED ─────────────────────────────────
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetById(Guid id)
    {
        var result = await _contentService.GetById(
            ContentCategory.JournalArticles, id);

        if (!result.Success) return BadRequest(result);

        // ✅ only return if published
        if (result.Data?.Status != "published")
            return NotFound(Response<ContentItemDTO>.Fail("Article not found"));

        return Ok(result);
    }
}
