using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CartDTO;
using MuuqWear.Model.DTO.ContentItemDTO;
using System;

namespace MuuqWear.Application.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ContentController : BaseController
{
    private readonly IContentService _contentService;

    public ContentController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet("{type}")]
    public async Task<ActionResult<Response<List<ContentItemDTO>>>> GetAll(ContentCategory type)
    {
        var result = await _contentService.GetAll(type);
        return HandleResponse(result); //  compiles + handles JWT_EXPIRED
    }

    [HttpGet("{type}/{id}")]
    public async Task<ActionResult<Response<ContentItemDTO>>> GetById(ContentCategory type, Guid id)
    {
        var result = await _contentService.GetById(type, id);
        return HandleResponse(result);
    }

    [HttpPost("{type}")]
    public async Task<ActionResult<Response<ContentItemDTO>>> Create(
        ContentCategory type,
        [FromBody] CreateContentItemDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(Response<ContentItemDTO>.Fail("Title is required"));

        var result = await _contentService.Create(type, request);
        return HandleResponse(result);
    }

    [HttpPut("{type}/{id}")]
    public async Task<ActionResult<Response<ContentItemDTO>>> Update(
        ContentCategory type,
        Guid id,
        [FromBody] UpdateContentItemDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(Response<ContentItemDTO>.Fail("Title is required"));

        var result = await _contentService.Update(type, id, request);
        return HandleResponse(result);
    }

    [HttpDelete("{type}/{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(ContentCategory type, Guid id)
    {
        var result = await _contentService.Delete(type, id);
        return HandleResponse(result);
    }

    [HttpPatch("{type}/{id}/publish")]
    public async Task<ActionResult<Response<ContentItemDTO>>> Publish(ContentCategory type, Guid id)
    {
        var result = await _contentService.Publish(type, id);
        return HandleResponse(result);
    }

    [HttpPatch("{type}/{id}/unpublish")]
    public async Task<ActionResult<Response<ContentItemDTO>>> Unpublish(ContentCategory type, Guid id)
    {
        var result = await _contentService.Unpublish(type, id);
        return HandleResponse(result);
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Response<string>>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Response<string>.Fail("No file provided"));

        var result = await _contentService.UploadImage(file);
        return HandleResponse(result);
    }

    [HttpGet]
    public async Task<ActionResult<Response<PaginatedResponse<ContentItemDTO>>>> GetPublished(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 6,
    [FromQuery] string? category = null)
    {
        // validate
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 6;

        var result = await _contentService.GetPublished(page, pageSize, category);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
    [HttpGet("design-history/published")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<List<ContentItemDTO>>>> GetPublishedDesignHistory()
    {
        var result = await _contentService.GetPublishedDesignHistory();
        return HandleResponse(result);
    }
}

