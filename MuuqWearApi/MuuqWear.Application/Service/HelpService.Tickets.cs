using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using MuuqWear.Model.Models.SupportTicket;

namespace MuuqWear.Application.Service;

public partial class HelpService
{
    // =============================================
    // UPDATE TICKET (ADMIN DRAWER)
    // =============================================
    public async Task<Response<SupportTicketDTO>> UpdateTicket(
        Guid ticketId, UpdateTicketDTO request)
    {
        try
        {
            if (ticketId == Guid.Empty)
                return Response<SupportTicketDTO>.Fail("Invalid ticket id");

            if (request == null)
                return Response<SupportTicketDTO>.Fail("Request body is required");

            var existing = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Limit(1)
                .Get();

            var ticket = existing.Models.FirstOrDefault();
            if (ticket == null)
                return Response<SupportTicketDTO>.Fail("Ticket not found");

            var status = ticket.Status;
            var priority = ticket.Priority;
            var team = ticket.Team;
            var assignedTo = ticket.AssignedTo;
            var assignedToName = ticket.AssignedToName;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var normalizedStatus = request.Status.Trim().ToLowerInvariant();
                if (!TicketStatus.All.Contains(normalizedStatus))
                    return Response<SupportTicketDTO>.Fail("Invalid status");
                status = normalizedStatus;
            }

            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                var normalizedPriority = request.Priority.Trim().ToLowerInvariant();
                if (!TicketPriority.All.Contains(normalizedPriority))
                    return Response<SupportTicketDTO>.Fail("Invalid priority");
                priority = normalizedPriority;
            }

            if (request.Team != null)
                team = NormalizeOptionalLabel(request.Team);

            // Keep assigned_to and assigned_to_name in sync on clear/reassign.
            if (request.AssignedToName != null)
            {
                assignedToName = NormalizeOptionalLabel(request.AssignedToName);
                if (assignedToName == null)
                    assignedTo = null;
            }

            if (request.AssignedTo.HasValue)
            {
                assignedTo = request.AssignedTo.Value == Guid.Empty
                    ? null
                    : request.AssignedTo.Value;

                // Clearing the id without an explicit name update also clears the name.
                if (assignedTo == null && request.AssignedToName == null)
                    assignedToName = null;
            }

            var now = DateTime.UtcNow;
            var result = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Set(t => t.Status, status)
                .Set(t => t.Priority, priority)
                .Set(t => t.Team!, team)
                .Set(t => t.AssignedTo!, assignedTo)
                .Set(t => t.AssignedToName!, assignedToName)
                .Set(t => t.UpdatedAt!, now)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<SupportTicketDTO>.Fail("Failed to update ticket");

            return Response<SupportTicketDTO>.SuccessResponse(
                await MapTicketToDtoAsync(updated, includeReplies: true),
                "Ticket updated");
        }
        catch (Exception)
        {
            return Response<SupportTicketDTO>.Fail("Unable to update ticket.");
        }
    }

    // =============================================
    // ADD TICKET REPLY (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketReplyDTO>> AddTicketReply(
        Guid ticketId,
        Guid? senderId,
        string senderName,
        string message)
    {
        try
        {
            if (ticketId == Guid.Empty)
                return Response<SupportTicketReplyDTO>.Fail("Invalid ticket id");

            if (string.IsNullOrWhiteSpace(message))
                return Response<SupportTicketReplyDTO>.Fail("Message is required");

            if (string.IsNullOrWhiteSpace(senderName))
                senderName = "Support Agent";

            var existing = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Limit(1)
                .Get();

            var ticket = existing.Models.FirstOrDefault();
            if (ticket == null)
                return Response<SupportTicketReplyDTO>.Fail("Ticket not found");

            var now = DateTime.UtcNow;
            var reply = new SupportTicketReply
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                SenderType = TicketSenderType.Agent,
                SenderId = senderId is { } id && id != Guid.Empty ? id : null,
                SenderName = senderName.Trim(),
                Message = message.Trim(),
                CreatedAt = now
            };

            var insertResult = await _client.From<SupportTicketReply>().Insert(reply);
            var inserted = insertResult.Models.FirstOrDefault();
            if (inserted == null)
                return Response<SupportTicketReplyDTO>.Fail("Failed to add reply");

            // Best-effort ticket metadata update after reply is persisted.
            try
            {
                var status = ticket.Status == TicketStatus.Open
                    ? TicketStatus.InProgress
                    : ticket.Status;

                var updateQuery = _client
                    .From<SupportTicket>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        ticketId.ToString())
                    .Set(t => t.Status, status)
                    .Set(t => t.UpdatedAt!, now);

                if (ticket.FirstResponseAt == null)
                    updateQuery = updateQuery.Set(t => t.FirstResponseAt!, now);

                await updateQuery.Update();
            }
            catch
            {
                // Reply already saved; metadata can be reconciled on next action.
            }

            return Response<SupportTicketReplyDTO>.SuccessResponse(
                MapReplyToDto(inserted),
                "Reply added");
        }
        catch (Exception)
        {
            return Response<SupportTicketReplyDTO>.Fail("Unable to add reply.");
        }
    }

    // =============================================
    // ASSIGN TICKET TO ME (ADMIN)
    // =============================================
    public async Task<Response<SupportTicketDTO>> AssignTicketToMe(
        Guid ticketId, Guid userId, string displayName)
    {
        try
        {
            if (ticketId == Guid.Empty)
                return Response<SupportTicketDTO>.Fail("Invalid ticket id");

            if (userId == Guid.Empty)
                return Response<SupportTicketDTO>.Fail("Authenticated user required");

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "Support Agent";

            var existing = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Limit(1)
                .Get();

            var ticket = existing.Models.FirstOrDefault();
            if (ticket == null)
                return Response<SupportTicketDTO>.Fail("Ticket not found");

            var status = ticket.Status == TicketStatus.Open
                ? TicketStatus.InProgress
                : ticket.Status;

            var now = DateTime.UtcNow;
            var result = await _client
                .From<SupportTicket>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Set(t => t.AssignedTo!, userId)
                .Set(t => t.AssignedToName!, displayName.Trim())
                .Set(t => t.Status, status)
                .Set(t => t.UpdatedAt!, now)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<SupportTicketDTO>.Fail("Failed to assign ticket");

            return Response<SupportTicketDTO>.SuccessResponse(
                await MapTicketToDtoAsync(updated, includeReplies: true),
                "Ticket assigned");
        }
        catch (Exception)
        {
            return Response<SupportTicketDTO>.Fail("Unable to assign ticket.");
        }
    }

    private async Task<List<SupportTicketReplyDTO>> GetRepliesForTicket(Guid ticketId)
    {
        try
        {
            var result = await _client
                .From<SupportTicketReply>()
                .Filter("ticket_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    ticketId.ToString())
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return result.Models.Select(MapReplyToDto).ToList();
        }
        catch
        {
            // Table/migration may not be applied yet — keep ticket detail usable.
            return [];
        }
    }

    private async Task<SupportTicketDTO> MapTicketToDtoAsync(
        SupportTicket ticket, bool includeReplies)
    {
        var dto = MapToDTO(ticket);
        if (includeReplies)
        {
            dto.Replies = await GetRepliesForTicket(ticket.Id);
            dto.ReplyCount = dto.Replies.Count;
        }

        return dto;
    }

    private static SupportTicketReplyDTO MapReplyToDto(SupportTicketReply reply) =>
        new()
        {
            Id = reply.Id,
            SenderType = reply.SenderType,
            SenderName = reply.SenderName,
            Message = reply.Message,
            CreatedAt = reply.CreatedAt
        };

    private static string? NormalizeOptionalLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Equals("Unassigned", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }
}
