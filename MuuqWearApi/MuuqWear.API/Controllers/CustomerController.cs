using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CustomerDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class CustomerController : BaseController
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // ─── GET ALL ──────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<Response<PaginatedResponse<CustomerDTO>>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _customerService.GetAll(search, page, pageSize);
        if (!result.Success) return BadRequest(result);
        return HandleResponse(result);
    }
}
