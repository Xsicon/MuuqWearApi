using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.Chat;
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

            // STEP 1: Get or create session
            if (request.SessionId.HasValue)
            {
                sessionId = request.SessionId.Value;
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
                    GuestName = request.GuestName,
                    GuestEmail = request.GuestEmail,
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
                : (request.GuestName ?? "Customer");

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
    public async Task<Response<List<ChatMessageDTO>>> GetMessages(Guid sessionId)
    {
        try
        {
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

            var dtos = new List<ChatSessionDTO>();

            foreach (var session in sessions.Models)
            {
                var lastMsgResult = await _client.From<ChatMessage>()
                    .Where(m => m.SessionId == session.Id)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var lastMsg = lastMsgResult.Models.FirstOrDefault();

                var customerName = session.UserId.HasValue
                    ? "Logged-in User"          // TODO: resolve real name later
                    : (session.GuestName ?? "Guest");

                dtos.Add(new ChatSessionDTO
                {
                    Id = session.Id,
                    CustomerName = customerName,
                    Status = session.Status,
                    LastActivity = session.UpdatedAt,
                    LastMessagePreview = lastMsg?.Message.Length > 50
                        ? lastMsg.Message.Substring(0, 50) + "..."
                        : lastMsg?.Message,
                    LastMessageSender = lastMsg?.SenderType,
                    CreatedAt = session.CreatedAt
                });
            }

            return Response<List<ChatSessionDTO>>.SuccessResponse(dtos, "Active sessions loaded");
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

    public async Task<Response<string>> GetSessionStatus(Guid sessionId)
    {
        try
        {
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
}
