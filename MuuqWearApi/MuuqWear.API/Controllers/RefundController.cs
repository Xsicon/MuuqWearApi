using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.RefundDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefundController : BaseController
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    // GET api/Refund/admin?status=&page=&pageSize=
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<PaginatedResponse<RefundDTO>>>> GetAllRefunds(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _refundService.GetAllRefunds(status, page, pageSize);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // GET api/Refund/admin/{refundId}
    [HttpGet("admin/{refundId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<RefundDTO>>> GetRefund(Guid refundId)
    {
        if (refundId == Guid.Empty)
            return BadRequest(Response<RefundDTO>.Fail("Invalid refund id"));

        var response = await _refundService.GetRefundById(refundId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // POST api/Refund/admin/{refundId}/process
    [HttpPost("admin/{refundId}/process")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<RefundDTO>>> ProcessRefund(Guid refundId)
    {
        if (refundId == Guid.Empty)
            return BadRequest(Response<RefundDTO>.Fail("Invalid refund id"));

        var response = await _refundService.ProcessRefund(refundId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
