using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.HelpCenterDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpController : BaseController
{
    private readonly IHelpCenterService _helpService;

    public HelpController(IHelpCenterService helpService)
    {
        _helpService = helpService;
    }

    // =============================================
    // SUBMIT TICKET
    // POST api/Help/ticket
    // ✅ public — anyone can submit a ticket
    // =============================================
    [HttpPost("ticket")]
    public async Task<ActionResult<Response<SupportTicketDTO>>> SubmitTicket(
        [FromBody] SubmitTicketDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Name is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Email is required"));

        if (string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Category is required"));

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Subject is required"));

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Message is required"));

        var response = await _helpService.SubmitTicket(request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ALL TICKETS
    // GET api/Help/admin/tickets
    // ✅ admin only
    // =============================================
    [HttpGet("admin/tickets")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<PaginatedResponse<SupportTicketDTO>>>> GetAllTickets(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _helpService.GetAllTickets(
            status, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET TICKET BY ID
    // GET api/Help/admin/tickets/{ticketId}
    // ✅ admin only
    // =============================================
    [HttpGet("admin/tickets/{ticketId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<SupportTicketDTO>>> GetTicketById(
        Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Invalid ticket id"));

        var response = await _helpService.GetTicketById(ticketId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE TICKET STATUS
    // PATCH api/Help/admin/tickets/{ticketId}/status
    // ✅ admin only
    // =============================================
    [HttpPatch("admin/tickets/{ticketId}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<SupportTicketDTO>>> UpdateTicketStatus(
        Guid ticketId,
        [FromBody] UpdateTicketStatusDTO request)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Invalid ticket id"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Status is required"));

        var response = await _helpService
            .UpdateTicketStatus(ticketId, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET STATS
    // GET api/Help/admin/stats
    // ✅ admin only
    // =============================================
    [HttpGet("admin/stats")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<TicketStatsDTO>>> GetStats()
    {
        var response = await _helpService.GetStats();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
