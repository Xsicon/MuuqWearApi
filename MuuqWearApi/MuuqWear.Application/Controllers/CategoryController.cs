using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<Response<List<CategoryDTO>>>> GetAll()
    {
        var response = await _categoryService.GetAll();
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Response<CategoryDTO>>> Add(AddCategoryDTO request)
    {
        var response = await _categoryService.Add(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }
}
