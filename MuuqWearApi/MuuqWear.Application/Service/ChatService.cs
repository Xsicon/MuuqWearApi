using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.Chat;
using MuuqWear.Model.Models.Profiles;
using MuuqWear.Model.Models.Chat;
using Supabase;

namespace MuuqWear.Application.Service;

public class ChatService : IChatService
{
    private readonly Client _client;

    public ChatService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // SEND MESSAGE  (creates session on first message)
    // =============================================
    public async Task<Response<ChatMessageDTO>> SendMessage(
        SendMessageRequest request, Guid? userId, bool isAdmin = false)
    {
        try
        {
            Guid sessionId;
            Profiles? senderProfile = null;

            if (!isAdmin && userId.HasValue)
            {
                var profileResult = await _client
                    .From<Profiles>()
                    .Where(p => p.Id == userId.Value)
                    .Limit(1)
                    .Get();

                senderProfile = profileResult.Models.FirstOrDefault();
            }

            // STEP 1: Get or create session
            if (request.SessionId.HasValue)
            {
                sessionId = request.SessionId.Value;

                // Enforce ownership for authenticated customers.
                if (!isAdmin)
                {
                    var existing = await _client.From<ChatSession>()
                        .Where(s => s.Id == sessionId)
                        .Limit(1)
                        .Get();

                    var existingSession = existing.Models.FirstOrDefault();
                    if (existingSession == null)
                        return Response<ChatMessageDTO>.Fail("Session not found");

                    var isOwner = userId.HasValue
                        ? existingSession.UserId == userId.Value
                        : existingSession.UserId == null;

                    if (!isOwner)
                        return Response<ChatMessageDTO>.Fail("Forbidden");
                }
            }
            else
            {
                // First message — only a customer can open a session
                if (isAdmin)
                    return Response<ChatMessageDTO>.Fail("Admins cannot create new sessions");

                var newSession = new ChatSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    // For authenticated users, persist name/email so admin inbox can always render customerEmail.
                    GuestName = senderProfile?.FullName ?? request.GuestName,
                    GuestEmail = senderProfile?.Email ?? request.GuestEmail,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _client.From<ChatSession>().Insert(newSession);
                sessionId = created.Models.First().Id;
            }

            // STEP 2: Determine sender display
            var senderType = isAdmin ? "admin" : "customer";
            var senderName = isAdmin
                ? "Support Team"
                : (senderProfile?.FullName ?? request.GuestName ?? "Customer");

            // STEP 3: Save the message
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                SenderType = senderType,
                SenderId = userId,
                SenderName = senderName,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _client.From<ChatMessage>().Insert(message);

            // STEP 4: Touch the session so it sorts to top of admin list
            var session = await _client.From<ChatSession>()
                .Where(s => s.Id == sessionId).Single();

            if (session != null)
            {
                session.UpdatedAt = DateTime.UtcNow;
                await _client.From<ChatSession>().Update(session);
            }

            // STEP 5: Return DTO (polling delivers it to the other side)
            var dto = new ChatMessageDTO
            {
                Id = message.Id,
                SessionId = message.SessionId,
                SenderType = message.SenderType,
                SenderName = message.SenderName ?? senderName,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead
            };

            return Response<ChatMessageDTO>.SuccessResponse(dto, "Message sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] SendMessage error: {ex.Message}");
            return Response<ChatMessageDTO>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // GET MESSAGES  (history + what polling calls)
    // =============================================
    public async Task<Response<List<ChatMessageDTO>>> GetMessages(
        Guid sessionId, Guid? userId, bool isAdmin = false)
    {
        try
        {
            // Ownership enforcement (authenticated customers only).
            if (!isAdmin)
            {
                var sessionResult = await _client.From<ChatSession>()
                    .Where(s => s.Id == sessionId)
                    .Limit(1)
                    .Get();

                var session = sessionResult.Models.FirstOrDefault();
                if (session == null)
                    return Response<List<ChatMessageDTO>>.Fail("Session not found");

                var isOwner = userId.HasValue
                    ? session.UserId == userId.Value
                    : session.UserId == null;

                if (!isOwner)
                    return Response<List<ChatMessageDTO>>.Fail("Forbidden");
            }

            var messages = await _client.From<ChatMessage>()
                .Where(m => m.SessionId == sessionId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var dtos = messages.Models.Select(m => new ChatMessageDTO
            {
                Id = m.Id,
                SessionId = m.SessionId,
                SenderType = m.SenderType,
                SenderName = m.SenderName ?? "Unknown",
                Message = m.Message,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead
            }).ToList();

            return Response<List<ChatMessageDTO>>.SuccessResponse(dtos, "Messages loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] GetMessages error: {ex.Message}");
            return Response<List<ChatMessageDTO>>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // GET ACTIVE SESSIONS  (admin dashboard)
    // =============================================
    public async Task<Response<List<ChatSessionDTO>>> GetActiveSessions()
    {
        try
        {
            var sessions = await _client.From<ChatSession>()
                .Where(s => s.Status == "active")
                .Order("updated_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var sessionsList = sessions.Models ?? new List<ChatSession>();
            var dtos = new List<ChatSessionDTO>();

            // Resolve emails/names for logged-in users in one query.
            var userIds = sessionsList
                .Where(s => s.UserId.HasValue)
                .Select(s => s.UserId!.Value)
                .Distinct()
                .ToList();

            var profileById = new Dictionary<Guid, Profiles>();
            if (userIds.Count > 0)
            {
                var profilesResult = await _client
                    .From<Profiles>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.In,
                        userIds.Select(id => id.ToString()).ToList())
                    .Get();

                profileById = (profilesResult.Models ?? new List<Profiles>())
                    .Where(p => p.Id.HasValue)
                    .ToDictionary(p => p.Id!.Value, p => p);
            }

            foreach (var session in sessionsList)
            {
                var lastMsgResult = await _client.From<ChatMessage>()
                    .Where(m => m.SessionId == session.Id)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var lastMsg = lastMsgResult.Models.FirstOrDefault();

                Profiles? profile = null;
                if (session.UserId.HasValue)
                    profileById.TryGetValue(session.UserId.Value, out profile);

                var customerName = profile != null
                    ? (profile.FullName ?? "Logged-in User")
                    : (session.GuestName ?? "Guest");

                var customerEmail = profile != null
                    ? profile.Email
                    : session.GuestEmail;

                dtos.Add(new ChatSessionDTO
                {
                    Id = session.Id,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    Status = session.Status,
                    LastActivity = session.UpdatedAt,
                    LastMessagePreview = lastMsg?.Message.Length > 50
                        ? lastMsg.Message.Substring(0, 50) + "..."
                        : lastMsg?.Message,
                    LastMessageSender = lastMsg?.SenderType,
                    CreatedAt = session.CreatedAt
                });
            }

            return Response<List<ChatSessionDTO>>.SuccessResponse(
                dtos,
                "Active sessions loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] GetActiveSessions error: {ex.Message}");
            return Response<List<ChatSessionDTO>>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // CLOSE SESSION  (admin)
    // =============================================
    public async Task<Response<bool>> CloseSession(Guid sessionId)
    {
        try
        {
            var session = await _client.From<ChatSession>()
                .Where(s => s.Id == sessionId).Single();

            if (session == null)
                return Response<bool>.Fail("Session not found");

            session.Status = "closed";
            session.ClosedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _client.From<ChatSession>().Update(session);

            return Response<bool>.SuccessResponse(true, "Session closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] CloseSession error: {ex.Message}");
            return Response<bool>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<string>> GetSessionStatus(
        Guid sessionId, Guid? userId, bool isAdmin = false)
    {
        try
        {
            if (!isAdmin)
            {
                var sessionResult = await _client.From<ChatSession>()
                    .Where(s => s.Id == sessionId)
                    .Limit(1)
                    .Get();

                var existingSession = sessionResult.Models.FirstOrDefault();
                if (existingSession == null)
                    return Response<string>.Fail("Session not found");

                var isOwner = userId.HasValue
                    ? existingSession.UserId == userId.Value
                    : existingSession.UserId == null;

                if (!isOwner)
                    return Response<string>.Fail("Forbidden");
            }

            var session = await _client.From<ChatSession>()
                .Where(s => s.Id == sessionId).Single();

            if (session == null)
                return Response<string>.Fail("Session not found");

            return Response<string>.SuccessResponse(session.Status, "Status fetched");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] GetSessionStatus error: {ex.Message}");
            return Response<string>.Fail($"Error: {ex.Message}");
        }
    }

    // =============================================
    // GET SESSION DETAILS (admin)
    // =============================================
    public async Task<Response<ChatSessionDTO>> GetSession(Guid sessionId)
    {
        try
        {
            var session = await _client
                .From<ChatSession>()
                .Where(s => s.Id == sessionId)
                .Single();

            if (session == null)
                return Response<ChatSessionDTO>.Fail("Session not found");

            Profiles? profile = null;
            if (session.UserId.HasValue)
            {
                var profileResult = await _client
                    .From<Profiles>()
                    .Where(p => p.Id == session.UserId.Value)
                    .Limit(1)
                    .Get();

                profile = profileResult.Models.FirstOrDefault();
            }

            var lastMsgResult = await _client.From<ChatMessage>()
                .Where(m => m.SessionId == session.Id)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            var lastMsg = lastMsgResult.Models.FirstOrDefault();

            var customerName = profile != null
                ? (profile.FullName ?? "Logged-in User")
                : (session.GuestName ?? "Guest");

            var customerEmail = profile != null
                ? profile.Email
                : session.GuestEmail;

            var dto = new ChatSessionDTO
            {
                Id = session.Id,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                Status = session.Status,
                LastActivity = session.UpdatedAt,
                LastMessagePreview = lastMsg?.Message.Length > 50
                    ? lastMsg.Message.Substring(0, 50) + "..."
                    : lastMsg?.Message,
                LastMessageSender = lastMsg?.SenderType,
                CreatedAt = session.CreatedAt
            };

            return Response<ChatSessionDTO>.SuccessResponse(dto, "Session loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] GetSession error: {ex.Message}");
            return Response<ChatSessionDTO>.Fail($"Error: {ex.Message}");
        }
    }
}
