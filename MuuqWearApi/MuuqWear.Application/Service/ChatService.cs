using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.Chat;
using MuuqWear.Model.Models.Profiles;
using MuuqWear.Model.Models.Chat;
using Microsoft.Extensions.Logging;
using Supabase;

namespace MuuqWear.Application.Service;

public class ChatService : IChatService
{
    private readonly Client _client;
    private readonly ILogger<ChatService> _logger;

    public ChatService(SupabaseAdminClientFactory factory, ILogger<ChatService> logger)
    {
        // Chat auth/ownership is enforced here + ChatController.
        // Do not use SupabaseClientFactory — it forwards the app JWT to PostgREST,
        // which is not a Supabase Auth token and causes RLS to hide chat_messages.
        _client = factory.CreateClient();
        _logger = logger;
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

                if (!isAdmin)
                {
                    var existing = await _client.From<ChatSession>()
                        .Where(s => s.Id == sessionId)
                        .Limit(1)
                        .Get();

                    var existingSession = existing.Models.FirstOrDefault();
                    if (existingSession == null)
                        return Response<ChatMessageDTO>.Fail("Session not found");

                    if (!ChatSessionAccess.CanAccessSession(
                            isAdmin, existingSession.UserId, userId))
                        return Response<ChatMessageDTO>.Fail("Forbidden");
                }
            }
            else
            {
                if (isAdmin)
                    return Response<ChatMessageDTO>.Fail("Admins cannot create new sessions");

                var newSession = new ChatSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    GuestName = senderProfile?.FullName ?? request.GuestName,
                    GuestEmail = senderProfile?.Email ?? request.GuestEmail,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var sessionInsert = await _client.From<ChatSession>().Insert(newSession);
                sessionId = sessionInsert.Models.First().Id;
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

            var messageInsert = await _client.From<ChatMessage>().Insert(message);
            var saved = messageInsert.Models.FirstOrDefault() ?? message;

            // STEP 4: Touch the session so it sorts to top of admin list
            var session = await _client.From<ChatSession>()
                .Where(s => s.Id == sessionId).Single();

            if (session != null)
            {
                session.UpdatedAt = DateTime.UtcNow;
                await _client.From<ChatSession>().Update(session);
            }

            var dto = new ChatMessageDTO
            {
                Id = saved.Id,
                SessionId = saved.SessionId,
                SenderType = saved.SenderType,
                SenderName = saved.SenderName ?? senderName,
                Message = saved.Message,
                CreatedAt = saved.CreatedAt,
                IsRead = saved.IsRead
            };

            return Response<ChatMessageDTO>.SuccessResponse(dto, "Message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed");
            return Response<ChatMessageDTO>.Fail("Unable to send message.");
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
            if (!isAdmin)
            {
                var sessionResult = await _client.From<ChatSession>()
                    .Where(s => s.Id == sessionId)
                    .Limit(1)
                    .Get();

                var session = sessionResult.Models.FirstOrDefault();
                if (session == null)
                    return Response<List<ChatMessageDTO>>.Fail("Session not found");

                if (!ChatSessionAccess.CanAccessSession(isAdmin, session.UserId, userId))
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
            _logger.LogError(ex, "GetMessages failed for session {SessionId}", sessionId);
            return Response<List<ChatMessageDTO>>.Fail("Unable to load messages.");
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

            var lastMessageBySession = await ChatMessageQueryHelper.LoadLatestMessagesBySessionAsync(
                _client,
                sessionsList.Select(s => s.Id).ToList());
            var messageStatsBySession = await ChatMessageQueryHelper.LoadMessageStatsBySessionAsync(
                _client,
                sessionsList.Select(s => s.Id).ToList());

            foreach (var session in sessionsList)
            {
                lastMessageBySession.TryGetValue(session.Id, out var lastMsg);
                messageStatsBySession.TryGetValue(session.Id, out var stats);

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
                    LastMessagePreview = TruncatePreview(lastMsg?.Message),
                    LastMessageSender = lastMsg?.SenderType,
                    MessageCount = stats?.Total ?? 0,
                    UnreadMessageCount = stats?.Unread ?? 0,
                    CreatedAt = session.CreatedAt
                });
            }

            return Response<List<ChatSessionDTO>>.SuccessResponse(
                dtos,
                "Active sessions loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveSessions failed");
            return Response<List<ChatSessionDTO>>.Fail("Unable to load active sessions.");
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
            _logger.LogError(ex, "CloseSession failed for session {SessionId}", sessionId);
            return Response<bool>.Fail("Unable to close session.");
        }
    }

    public async Task<Response<string>> GetSessionStatus(
        Guid sessionId, Guid? userId, bool isAdmin = false)
    {
        try
        {
            var sessionResult = await _client.From<ChatSession>()
                .Where(s => s.Id == sessionId)
                .Limit(1)
                .Get();

            var session = sessionResult.Models.FirstOrDefault();
            if (session == null)
                return Response<string>.Fail("Session not found");

            if (!ChatSessionAccess.CanAccessSession(isAdmin, session.UserId, userId))
                return Response<string>.Fail("Forbidden");

            return Response<string>.SuccessResponse(session.Status, "Status fetched");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSessionStatus failed for session {SessionId}", sessionId);
            return Response<string>.Fail("Unable to load session status.");
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

            var lastMsg = (await ChatMessageQueryHelper.LoadLatestMessagesBySessionAsync(_client, [session.Id]))
                .GetValueOrDefault(session.Id);
            var messageStats = (await ChatMessageQueryHelper.LoadMessageStatsBySessionAsync(_client, [session.Id]))
                .GetValueOrDefault(session.Id);

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
                LastMessagePreview = TruncatePreview(lastMsg?.Message),
                LastMessageSender = lastMsg?.SenderType,
                MessageCount = messageStats?.Total ?? 0,
                UnreadMessageCount = messageStats?.Unread ?? 0,
                CreatedAt = session.CreatedAt
            };

            return Response<ChatSessionDTO>.SuccessResponse(dto, "Session loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSession failed for session {SessionId}", sessionId);
            return Response<ChatSessionDTO>.Fail("Unable to load session.");
        }
    }

    private static string? TruncatePreview(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return message.Length > 50
            ? message[..50] + "..."
            : message;
    }
}
