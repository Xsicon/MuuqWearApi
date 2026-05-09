using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.HelpCenterDTO;
using MuuqWear.Model.Models;
using System.Text.Json;

namespace MuuqWear.Application.Service;

public class HelpService : IHelpCenterService
{
    private readonly Supabase.Client _client;

    public HelpService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // SUBMIT TICKET
    // =============================================
    public async Task<Response<SupportTicketDTO>> SubmitTicket(
        SubmitTicketDTO request)
    {
        try
        {
            // Step 1 — auto-generate ticket number
            var ticketNumber = $"TKT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            // Step 2 — auto-assign priority from category
            var priority = TicketPriority.FromCategory(request.Category);

            // Step 3 — insert ticket
            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                TicketNumber = ticketNumber,
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                Category = request.Category.Trim(),
                Subject = request.Subject.Trim(),
                Message = request.Message.Trim(),
                Priority = priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _client
                .From<SupportTicket>()
                .Insert(ticket);

            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<SupportTicketDTO>.Fail(
                    "Failed to submit ticket. Please try again.");

            return Response<SupportTicketDTO>.SuccessResponse(
                MapToDTO(inserted),
                "Ticket submitted successfully");
        }
        catch (Exception ex)
        {
            return Response<SupportTicketDTO>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET ALL TICKETS (ADMIN)
    // =============================================
    public async Task<Response<PaginatedResponse<SupportTicketDTO>>> GetAllTickets(
        string? status, int page, int pageSize)
    {
        try
        {
            var statusParam = status?.Trim() ?? "";
            var offset = (page - 1) * pageSize;

            // Step 1 — count
            var countResult = await _client.Rpc(
                "get_support_tickets_count",
                new Dictionary<string, object>
                {
                    { "p_status", statusParam }
                });

            var totalCount = 0;
            int.TryParse(countResult.Content?.Trim('"'), out totalCount);

            // Step 2 — fetch paginated data
            var dataResult = await _client.Rpc(
                "get_support_tickets",
                new Dictionary<string, object>
                {
                    { "p_status",    statusParam },
                    { "p_page_size", pageSize    },
                    { "p_offset",    offset      }
                });

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var tickets = JsonSerializer
                .Deserialize<List<SupportTicketDTO>>(
                    dataResult.Content ?? "[]", options)
                ?? new List<SupportTicketDTO>();

            var totalPages = (int)Math.Ceiling(
                (double)totalCount / pageSize);

            var paginated = new PaginatedResponse<SupportTicketDTO>
            {
                Data = tickets,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<SupportTicketDTO>>
                .SuccessResponse(paginated, "Tickets fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<SupportTicketDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET TICKET BY ID (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketDTO>> GetTicketById(
        Guid ticketId)
    {
        try
        {
            var ticket = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Single();

            if (ticket == null)
                return Response<SupportTicketDTO>.Fail(
                    "Ticket not found");

            return Response<SupportTicketDTO>
                .SuccessResponse(MapToDTO(ticket), "Ticket fetched");
        }
        catch (Exception ex)
        {
            return Response<SupportTicketDTO>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE TICKET STATUS (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketDTO>> UpdateTicketStatus(
        Guid ticketId, string status)
    {
        try
        {
            if (!TicketStatus.All.Contains(status))
                return Response<SupportTicketDTO>.Fail("Invalid status");

            var result = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Set(t => t.Status, status)
                .Set(t => t.UpdatedAt!, DateTime.UtcNow)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<SupportTicketDTO>.Fail(
                    "Ticket not found");

            return Response<SupportTicketDTO>
                .SuccessResponse(MapToDTO(updated), "Status updated");
        }
        catch (Exception ex)
        {
            return Response<SupportTicketDTO>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET STATS (ADMIN)
    // =============================================
    public async Task<Response<TicketStatsDTO>> GetStats()
    {
        try
        {
            var result = await _client.Rpc(
                "get_support_ticket_stats",
                new Dictionary<string, object>());

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var stats = JsonSerializer
                .Deserialize<List<TicketStatsDTO>>(
                    result.Content ?? "[]", options)
                ?.FirstOrDefault()
                ?? new TicketStatsDTO();

            return Response<TicketStatsDTO>
                .SuccessResponse(stats, "Stats fetched");
        }
        catch (Exception ex)
        {
            return Response<TicketStatsDTO>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // PRIVATE HELPER
    // =============================================
    // ✅ single responsibility — maps model to DTO
    private static SupportTicketDTO MapToDTO(SupportTicket t) =>
        new SupportTicketDTO
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            Name = t.Name,
            Email = t.Email,
            Category = t.Category,
            Subject = t.Subject,
            Message = t.Message,
            Priority = t.Priority,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
}
