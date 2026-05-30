using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.Chat;
using MuuqWear.Model.Models.Chat;

namespace MuuqWear.Application.Interfaces;

public interface IChatService
{
    Task<Response<ChatMessageDTO>> SendMessage(SendMessageRequest request, Guid? userId, bool isAdmin = false);
    Task<Response<List<ChatMessageDTO>>> GetMessages(Guid sessionId);
    Task<Response<List<ChatSessionDTO>>> GetActiveSessions();
    Task<Response<bool>> CloseSession(Guid sessionId);
    Task<Response<string>> GetSessionStatus(Guid sessionId);
}
