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
    public async Task<ActionResult<Response<PaginatedResponse<ProductDTO>>>> GetAll(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        search = search?.Replace("%", "\\%").Replace("_", "\\_");
        if (string.IsNullOrWhiteSpace(search)) search = null;

        var response = await _productService.GetAll(page, pageSize, search);

        // ✅ if page exceeds total pages → return last page
        if (response.Success && response.Data != null)
        {
            var totalPages = response.Data.TotalPages;
            if (totalPages > 0 && page > totalPages)
            {
                response = await _productService.GetAll(totalPages, pageSize, search);
            }
        }

        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("home")]
    public async Task<ActionResult<Response<HomeProductsDTO>>> GetHomeProducts()
    {
        var response = await _productService.GetHomeProducts();
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

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<ProductDTO>>> GetById(Guid id)
    {
        // validate id is not empty Guid
        // e.g. 00000000-0000-0000-0000-000000000000
        if (id == Guid.Empty)
            return BadRequest(Response<ProductDTO>.Fail("Invalid product id"));

        var response = await _productService.GetById(id);

        if (!response.Success)
            return NotFound(response); // 404 if product not found ✅

        return Ok(response);
    }

    [HttpGet("{id}/related")]
    public async Task<ActionResult<Response<List<ProductDTO>>>> GetRelated(Guid id)
    {
        // validate id
        if (id == Guid.Empty)
            return BadRequest(Response<List<ProductDTO>>.Fail("Invalid product id"));

        var productResponse = await _productService.GetById(id);

        if (!productResponse.Success)
            return NotFound(Response<List<ProductDTO>>.Fail("Product not found"));

        var categoryId = productResponse.Data?.CategoryId;

        var response = await _productService.GetRelated(id, categoryId);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("images/add")]
    public async Task<ActionResult<Response<ProductImageDTO>>> AddProductImage(AddProductImageDTO request)
    {
        if (request.ProductId == Guid.Empty)
            return BadRequest(Response<ProductImageDTO>.Fail("Invalid product id"));

        if (string.IsNullOrEmpty(request.ImageUrl))
            return BadRequest(Response<ProductImageDTO>.Fail("Image URL is required"));

        var response = await _productService.AddProductImage(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("images/{imageId}")]
    public async Task<ActionResult<Response<bool>>> DeleteProductImage(Guid imageId)
    {
        if (imageId == Guid.Empty)
            return BadRequest(Response<bool>.Fail("Invalid image id"));

        var response = await _productService.DeleteProductImage(imageId);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }
}
