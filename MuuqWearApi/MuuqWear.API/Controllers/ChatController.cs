using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.Chat;
using MuuqWear.Model.Models.Chat;
using System.Security.Claims;
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

    private static bool IsChatAdmin(ClaimsPrincipal user) =>
        AdminRoleClaims.CanActAsChatAdmin(user);

    /// <summary>
    /// Send a message (customer or admin). First message creates the session.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("send")]
    public async Task<ActionResult<Response<ChatMessageDTO>>> SendMessage(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Response<ChatMessageDTO>.Fail("Message cannot be empty"));

        Guid? userId = ResolveOptionalUserId();

        if (!userId.HasValue && string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(Response<ChatMessageDTO>.Fail("Guest name is required"));

        if (!userId.HasValue
            && !request.SessionId.HasValue
            && string.IsNullOrWhiteSpace(request.GuestEmail))
        {
            return BadRequest(Response<ChatMessageDTO>.Fail("Guest email is required"));
        }

        var isAdmin = User.Identity?.IsAuthenticated == true && IsChatAdmin(User);

        var result = await _chatService.SendMessage(request, userId, isAdmin);
        if (!result.Success
            && result.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, result);

        return HandleResponse(result);
    }

    /// <summary>
    /// Get all messages for a session (history + polling).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("messages/{sessionId}")]
    public async Task<ActionResult<Response<List<ChatMessageDTO>>>> GetMessages(Guid sessionId)
    {
        var userId = ResolveOptionalUserId();
        var isAdmin = User.Identity?.IsAuthenticated == true && IsChatAdmin(User);

        var result = await _chatService.GetMessages(sessionId, userId, isAdmin);
        if (!result.Success
            && result.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, result);

        return HandleResponse(result);
    }
    /// <summary>
    /// Get active sessions for the admin dashboard.
    /// </summary>
    [HttpGet("active-sessions")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<List<ChatSessionDTO>>>> GetActiveSessions()
    {
        var result = await _chatService.GetActiveSessions();
        return HandleResponse(result);
    }

    /// <summary>
    /// Close a session (admin only).
    /// </summary>
    [HttpPost("close/{sessionId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<bool>>> CloseSession(Guid sessionId)
    {
        var result = await _chatService.CloseSession(sessionId);
        return HandleResponse(result);
    }

    /// <summary>
    /// Get a session's current status (used by the customer to detect closure).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("session/{sessionId}/status")]
    public async Task<ActionResult<Response<string>>> GetSessionStatus(Guid sessionId)
    {
        var userId = ResolveOptionalUserId();
        var isAdmin = User.Identity?.IsAuthenticated == true && IsChatAdmin(User);

        var result = await _chatService.GetSessionStatus(sessionId, userId, isAdmin);
        if (!result.Success
            && result.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, result);

        return HandleResponse(result);
    }
    /// <summary>
    /// Load full session details (admin only).
    /// Includes customerEmail for admin UX.
    /// </summary>
    [HttpGet("session/{sessionId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<ChatSessionDTO>>> GetSession(Guid sessionId)
    {
        var result = await _chatService.GetSession(sessionId);
        if (!result.Success
            && result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            return NotFound(result);

        return HandleResponse(result);
    }

    private Guid? ResolveOptionalUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var id = GetUserId();
        return id == Guid.Empty ? null : id;
    }
}