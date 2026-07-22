using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.CustomerDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : BaseController
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomerNotesRead)]
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

    [HttpGet("{customerId:guid}/notes")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomerNotesRead)]
    public async Task<ActionResult<Response<List<CustomerNoteDTO>>>> GetNotes(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return BadRequest(Response<List<CustomerNoteDTO>>.Fail("Invalid customer id"));

        var result = await _customerService.GetNotes(customerId);
        if (!result.Success && result.Message == "Customer not found")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
    }

    [HttpPost("{customerId:guid}/notes")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomers)]
    public async Task<ActionResult<Response<CustomerNoteDTO>>> CreateNote(
        Guid customerId,
        [FromBody] CreateCustomerNoteDTO request)
    {
        if (customerId == Guid.Empty)
            return BadRequest(Response<CustomerNoteDTO>.Fail("Invalid customer id"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CustomerNoteDTO>.Fail("Not authenticated"));

        if (request == null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(Response<CustomerNoteDTO>.Fail("Note body is required"));

        var trimmed = request.Body.Trim();
        if (trimmed.Length < 1 || trimmed.Length > 4000)
        {
            return BadRequest(Response<CustomerNoteDTO>.Fail(
                "Note body must be between 1 and 4000 characters"));
        }

        var result = await _customerService.CreateNote(customerId, trimmed, userId);
        if (!result.Success && result.Message == "Customer not found")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
    }
}
