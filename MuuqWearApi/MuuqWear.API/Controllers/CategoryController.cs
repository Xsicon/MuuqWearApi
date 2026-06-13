using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;

namespace MuuqWear.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class CategoryController : BaseController
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
        return HandleResponse(response);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Response<CategoryDTO>>> Add(AddCategoryDTO request)
    {
        var response = await _categoryService.Add(request);
        if (!response.Success)
            return BadRequest(response);
        return HandleResponse(response);
    }
}
