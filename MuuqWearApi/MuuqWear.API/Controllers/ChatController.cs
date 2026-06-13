using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.Chat;
using MuuqWear.Model.Models.Chat;

namespace MuuqWear.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Send a message (customer or admin). First message creates the session.
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<Response<ChatMessageDTO>>> SendMessage(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Response<ChatMessageDTO>.Fail("Message cannot be empty"));

        // Resolve user (null = guest)
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
            userId = GetUserId();

        // Guest must supply a name
        if (!userId.HasValue && string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(Response<ChatMessageDTO>.Fail("Guest name is required"));

        var isAdmin = User.IsInRole("admin");

        var result = await _chatService.SendMessage(request, userId, isAdmin);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get all messages for a session (history + polling).
    /// </summary>
    [HttpGet("messages/{sessionId}")]
    public async Task<ActionResult<Response<List<ChatMessageDTO>>>> GetMessages(Guid sessionId)
    {
        var result = await _chatService.GetMessages(sessionId);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get active sessions for the admin dashboard.
    /// </summary>
    [HttpGet("active-sessions")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<List<ChatSessionDTO>>>> GetActiveSessions()
    {
        var result = await _chatService.GetActiveSessions();
        return HandleResponse(result);
    }

    /// <summary>
    /// Close a session (admin only).
    /// </summary>
    [HttpPost("close/{sessionId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<bool>>> CloseSession(Guid sessionId)
    {
        var result = await _chatService.CloseSession(sessionId);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get a session's current status (used by the customer to detect closure).
    /// </summary>
    [HttpGet("session/{sessionId}/status")]
    public async Task<ActionResult<Response<string>>> GetSessionStatus(Guid sessionId)
    {
        var result = await _chatService.GetSessionStatus(sessionId);
        return HandleResponse(result);
    }
}
