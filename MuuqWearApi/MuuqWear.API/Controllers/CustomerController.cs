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
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            if (normalized is not ("active" or "suspended" or "all"))
            {
                return BadRequest(Response<PaginatedResponse<CustomerDTO>>
                    .Fail("status must be active, suspended, or all"));
            }

            if (normalized == "all")
                status = null;
            else
                status = normalized;
        }

        var result = await _customerService.GetAll(search, page, pageSize, status);
        if (!result.Success) return BadRequest(result);
        return HandleResponse(result);
    }

    [HttpGet("{customerId:guid}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomerNotesRead)]
    public async Task<ActionResult<Response<CustomerDTO>>> GetById(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return BadRequest(Response<CustomerDTO>.Fail("Invalid customer id"));

        var result = await _customerService.GetById(customerId);
        if (!result.Success && result.Message == "Customer not found")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
    }

    [HttpPatch("{customerId:guid}/suspend")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomers)]
    public async Task<ActionResult<Response<CustomerDTO>>> Suspend(
        Guid customerId,
        [FromBody] SuspendCustomerDTO request)
    {
        if (customerId == Guid.Empty)
            return BadRequest(Response<CustomerDTO>.Fail("Invalid customer id"));

        var adminId = GetUserId();
        if (adminId == Guid.Empty)
            return Unauthorized(Response<CustomerDTO>.Fail("Not authenticated"));

        if (adminId == customerId)
            return BadRequest(Response<CustomerDTO>.Fail("You cannot suspend your own account"));

        if (request == null)
            return BadRequest(Response<CustomerDTO>.Fail("Request body is required"));

        var result = await _customerService.Suspend(customerId, request, adminId);
        if (!result.Success && result.Message == "Customer not found")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);

        return HandleResponse(result);
    }

    [HttpPatch("{customerId:guid}/reactivate")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminCustomers)]
    public async Task<ActionResult<Response<CustomerDTO>>> Reactivate(
        Guid customerId,
        [FromBody] ReactivateCustomerDTO? request = null)
    {
        if (customerId == Guid.Empty)
            return BadRequest(Response<CustomerDTO>.Fail("Invalid customer id"));

        var adminId = GetUserId();
        if (adminId == Guid.Empty)
            return Unauthorized(Response<CustomerDTO>.Fail("Not authenticated"));

        var result = await _customerService.Reactivate(customerId, request, adminId);
        if (!result.Success && result.Message == "Customer not found")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);

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
