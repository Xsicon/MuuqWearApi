using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO.ProductDTO;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;

namespace MuuqWear.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<Response<List<ProductDTO>>>> GetAll()
    {
        var response = await _productService.GetAll();
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Response<ProductDTO>>> Add(AddProductDTO request)
    {
        var response = await _productService.Add(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("upload-image")]
    public async Task<ActionResult<Response<string>>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Response<string>.Fail("No file provided"));

        var response = await _productService.UploadImage(file);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut("update/{id}")]
    public async Task<ActionResult<Response<ProductDTO>>> Update(Guid id, UpdateProductDTO request)
    {
        var response = await _productService.Update(id, request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("delete/{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(Guid id)
    {
        var response = await _productService.Delete(id);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }
}
