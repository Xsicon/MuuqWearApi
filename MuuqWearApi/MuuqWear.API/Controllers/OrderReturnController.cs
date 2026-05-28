using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.OrdeReturnDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnController : BaseController
{
    private readonly IOrderReturnService _returnService;

    public ReturnController(IOrderReturnService returnService)
    {
        _returnService = returnService;
    }

    // =============================================
    // SUBMIT RETURN
    // POST api/Return/submit
    // any logged-in user can submit 
    // =============================================
    [HttpPost("submit")]
    [Authorize]
    public async Task<ActionResult<Response<OrderReturnDTO>>> SubmitReturn(
        [FromBody] SubmitReturnDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderNumber))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Order number is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Email is required"));

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.ItemsToReturn))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Items to return is required"));

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Reason is required"));

        var response = await _returnService.SubmitReturn(request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ALL RETURNS (ADMIN)
    // GET api/Return/admin?status=&page=&pageSize=
    // =============================================
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<PaginatedResponse<OrderReturnDTO>>>> GetAllReturns(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _returnService.GetAllReturns(
            status, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE RETURN STATUS (ADMIN)
    // PATCH api/Return/admin/{returnId}/status
    // =============================================
    [HttpPatch("admin/{returnId}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<OrderReturnDTO>>> UpdateReturnStatus(
        Guid returnId,
        [FromBody] UpdateReturnStatusDTO request)
    {
        if (returnId == Guid.Empty)
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Invalid return id"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<OrderReturnDTO>
                .Fail("Status is required"));

        var response = await _returnService
            .UpdateReturnStatus(returnId, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
