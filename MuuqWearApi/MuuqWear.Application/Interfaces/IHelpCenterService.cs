using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;

namespace MuuqWear.Application.Interfaces;

public interface IHelpCenterService
{
    // ─── Public ───────────────────────────────────────────────
    Task<Response<SupportTicketDTO>> SubmitTicket(SubmitTicketDTO request);

    // ─── Admin ────────────────────────────────────────────────
    Task<Response<PaginatedResponse<SupportTicketDTO>>> GetAllTickets(
        string? status, int page, int pageSize);
    Task<Response<SupportTicketDTO>> GetTicketById(Guid ticketId);
    Task<Response<SupportTicketDTO>> UpdateTicketStatus(
        Guid ticketId, string status);
    Task<Response<TicketStatsDTO>> GetStats();
}
