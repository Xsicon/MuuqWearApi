using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using MuuqWear.Model.Models.SupportTicket;
using System.Text.Json;

namespace MuuqWear.Application.Service;

public partial class HelpService : IHelpCenterService
{
    private readonly Supabase.Client _client;

    public HelpService(SupabaseAdminClientFactory factory)
    {
        // Auth is enforced on HelpController. Use service-role client so
        // custom app JWTs are not forwarded to PostgREST (RLS would hide rows).
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
            var ticketNumber = $"TKT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var priority = TicketPriority.FromCategory(request.Category);

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

            // Seed original customer message into the reply thread for continuity.
            try
            {
                await _client.From<SupportTicketReply>().Insert(new SupportTicketReply
                {
                    Id = Guid.NewGuid(),
                    TicketId = inserted.Id,
                    SenderType = TicketSenderType.Customer,
                    SenderId = null,
                    SenderName = inserted.Name,
                    Message = inserted.Message,
                    CreatedAt = inserted.CreatedAt ?? DateTime.UtcNow
                });
            }
            catch
            {
                // Reply table may not exist yet — ticket submit still succeeds.
            }

            return Response<SupportTicketDTO>.SuccessResponse(
                MapToDTO(inserted),
                "Ticket submitted successfully");
        }
        catch (Exception)
        {
            return Response<SupportTicketDTO>
                .Fail("Unable to submit ticket.");
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

            var countResult = await _client.Rpc(
                "get_support_tickets_count",
                new Dictionary<string, object>
                {
                    { "p_status", statusParam }
                });

            var totalCount = 0;
            int.TryParse(countResult.Content?.Trim('"'), out totalCount);

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
        catch (Exception)
        {
            return Response<PaginatedResponse<SupportTicketDTO>>
                .Fail("Unable to load tickets.");
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
                .SuccessResponse(
                    await MapTicketToDtoAsync(ticket, includeReplies: true),
                    "Ticket fetched");
        }
        catch (Exception)
        {
            return Response<SupportTicketDTO>
                .Fail("Unable to load ticket.");
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
                .SuccessResponse(
                    await MapTicketToDtoAsync(updated, includeReplies: true),
                    "Status updated");
        }
        catch (Exception)
        {
            return Response<SupportTicketDTO>
                .Fail("Unable to update ticket status.");
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
        catch (Exception)
        {
            return Response<TicketStatsDTO>
                .Fail("Unable to load ticket stats.");
        }
    }

    private static SupportTicketDTO MapToDTO(SupportTicket t) =>
        new()
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
            Team = t.Team,
            AssignedTo = t.AssignedTo,
            AssignedToName = t.AssignedToName,
            FirstResponseAt = t.FirstResponseAt,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
}
