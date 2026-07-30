using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using System.Security.Claims;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpController : BaseController
{
    private readonly IHelpCenterService _helpService;

    public HelpController(IHelpCenterService helpService)
    {
        _helpService = helpService;
    }

    // =============================================
    // SUBMIT TICKET
    // POST api/Help/ticket
    //  public — anyone can submit a ticket
    // =============================================
    [HttpPost("ticket")]
    public async Task<ActionResult<Response<SupportTicketDTO>>> SubmitTicket(
        [FromBody] SubmitTicketDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Name is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Email is required"));

        if (string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Category is required"));

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Subject is required"));

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Message is required"));

        var response = await _helpService.SubmitTicket(request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ALL TICKETS
    // GET api/Help/admin/tickets
    //  admin only
    // =============================================
    [HttpGet("admin/tickets")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<PaginatedResponse<SupportTicketDTO>>>> GetAllTickets(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _helpService.GetAllTickets(
            status, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET TICKET BY ID
    // GET api/Help/admin/tickets/{ticketId}
    //  admin only
    // =============================================
    [HttpGet("admin/tickets/{ticketId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<SupportTicketDTO>>> GetTicketById(
        Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Invalid ticket id"));

        var response = await _helpService.GetTicketById(ticketId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE TICKET STATUS
    // PATCH api/Help/admin/tickets/{ticketId}/status
    //  admin only
    // =============================================
    [HttpPatch("admin/tickets/{ticketId}/status")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<SupportTicketDTO>>> UpdateTicketStatus(
        Guid ticketId,
        [FromBody] UpdateTicketStatusDTO request)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Invalid ticket id"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<SupportTicketDTO>
                .Fail("Status is required"));

        var response = await _helpService
            .UpdateTicketStatus(ticketId, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE TICKET (DRAWER)
    // PATCH api/Help/admin/tickets/{ticketId}
    // =============================================
    [HttpPatch("admin/tickets/{ticketId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<SupportTicketDTO>>> UpdateTicket(
        Guid ticketId,
        [FromBody] UpdateTicketDTO request)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>.Fail("Invalid ticket id"));

        var response = await _helpService.UpdateTicket(ticketId, request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // ADD TICKET REPLY
    // POST api/Help/admin/tickets/{ticketId}/replies
    // =============================================
    [HttpPost("admin/tickets/{ticketId}/replies")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<SupportTicketReplyDTO>>> AddTicketReply(
        Guid ticketId,
        [FromBody] AddTicketReplyDTO request)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketReplyDTO>.Fail("Invalid ticket id"));

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Response<SupportTicketReplyDTO>.Fail("Message is required"));

        var userId = GetUserId();
        var response = await _helpService.AddTicketReply(
            ticketId,
            userId == Guid.Empty ? null : userId,
            ResolveAuthorName(),
            request.Message);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // ASSIGN TICKET TO ME
    // POST api/Help/admin/tickets/{ticketId}/assign-me
    // =============================================
    [HttpPost("admin/tickets/{ticketId}/assign-me")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<SupportTicketDTO>>> AssignTicketToMe(
        Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            return BadRequest(Response<SupportTicketDTO>.Fail("Invalid ticket id"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(Response<SupportTicketDTO>
                .Fail("Authenticated user required"));

        var response = await _helpService.AssignTicketToMe(
            ticketId, userId, ResolveAuthorName());

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET STATS
    // GET api/Help/admin/stats
    //  admin only
    // =============================================
    [HttpGet("admin/stats")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<TicketStatsDTO>>> GetStats()
    {
        var response = await _helpService.GetStats();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET PUBLISHED ARTICLES
    // GET api/Help/articles
    // =============================================
    [HttpGet("articles")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<PaginatedResponse<HelpArticleDTO>>>> GetPublishedArticles(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _helpService.GetPublishedArticles(
            category, search, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET PUBLISHED ARTICLE BY ID
    // GET api/Help/articles/{articleId}
    // =============================================
    [HttpGet("articles/{articleId}")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<HelpArticleDTO>>> GetPublishedArticleById(
        Guid articleId)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleDTO>.Fail("Invalid article id"));

        var response = await _helpService.GetPublishedArticleById(articleId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ADMIN ARTICLES
    // GET api/Help/admin/articles
    // =============================================
    [HttpGet("admin/articles")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<PaginatedResponse<HelpArticleDTO>>>> GetAdminArticles(
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _helpService.GetAdminArticles(
            category, status, search, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ADMIN ARTICLE BY ID
    // GET api/Help/admin/articles/{articleId}
    // =============================================
    [HttpGet("admin/articles/{articleId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleDTO>>> GetAdminArticleById(
        Guid articleId)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleDTO>.Fail("Invalid article id"));

        var response = await _helpService.GetAdminArticleById(
            articleId, ResolveVoterKey());
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // CREATE ARTICLE
    // POST api/Help/admin/articles
    // =============================================
    [HttpPost("admin/articles")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleDTO>>> CreateArticle(
        [FromBody] SaveHelpArticleDTO request)
    {
        var response = await _helpService.CreateArticle(request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE ARTICLE
    // PUT api/Help/admin/articles/{articleId}
    // =============================================
    [HttpPut("admin/articles/{articleId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleDTO>>> UpdateArticle(
        Guid articleId,
        [FromBody] SaveHelpArticleDTO request)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleDTO>.Fail("Invalid article id"));

        var response = await _helpService.UpdateArticle(articleId, request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE ARTICLE STATUS
    // PATCH api/Help/admin/articles/{articleId}/status
    // =============================================
    [HttpPatch("admin/articles/{articleId}/status")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleDTO>>> UpdateArticleStatus(
        Guid articleId,
        [FromBody] UpdateHelpArticleStatusDTO request)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleDTO>.Fail("Invalid article id"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<HelpArticleDTO>.Fail("Status is required"));

        var response = await _helpService.UpdateArticleStatus(
            articleId, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // DELETE ARTICLE
    // DELETE api/Help/admin/articles/{articleId}
    // =============================================
    [HttpDelete("admin/articles/{articleId}")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<bool>>> DeleteArticle(Guid articleId)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<bool>.Fail("Invalid article id"));

        var response = await _helpService.DeleteArticle(articleId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPLOAD IMAGE
    // POST api/Help/admin/upload-image
    // =============================================
    [HttpPost("admin/upload-image")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Response<string>>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Response<string>.Fail("No file provided"));

        var response = await _helpService.UploadImage(file);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // ADD ARTICLE COMMENT
    // POST api/Help/admin/articles/{articleId}/comments
    // =============================================
    [HttpPost("admin/articles/{articleId}/comments")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleCommentDTO>>> AddArticleComment(
        Guid articleId,
        [FromBody] AddHelpArticleCommentDTO request)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleCommentDTO>.Fail("Invalid article id"));

        if (request == null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(Response<HelpArticleCommentDTO>.Fail("Comment body is required"));

        var userId = GetUserId();
        var authorName = ResolveAuthorName();

        var response = await _helpService.AddArticleComment(
            articleId,
            userId == Guid.Empty ? null : userId,
            authorName,
            request.Body);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // SET ARTICLE VOTE
    // POST api/Help/admin/articles/{articleId}/vote
    // =============================================
    [HttpPost("admin/articles/{articleId}/vote")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleEngagementDTO>>> SetArticleVote(
        Guid articleId,
        [FromBody] HelpArticleVoteRequestDTO request)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleEngagementDTO>.Fail("Invalid article id"));

        var voterKey = ResolveVoterKey();
        if (string.IsNullOrWhiteSpace(voterKey))
            return Unauthorized(Response<HelpArticleEngagementDTO>
                .Fail("Authenticated user required"));

        if (request == null || string.IsNullOrWhiteSpace(request.Vote))
            return BadRequest(Response<HelpArticleEngagementDTO>
                .Fail("Vote is required"));

        var response = await _helpService.SetArticleVote(
            articleId, voterKey, request.Vote);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ARTICLE ENGAGEMENT
    // GET api/Help/admin/articles/{articleId}/engagement
    // =============================================
    [HttpGet("admin/articles/{articleId}/engagement")]
    [Authorize(Policy = AdminAuthorizationPolicies.AdminSupport)]
    public async Task<ActionResult<Response<HelpArticleEngagementDTO>>> GetArticleEngagement(
        Guid articleId)
    {
        if (articleId == Guid.Empty)
            return BadRequest(Response<HelpArticleEngagementDTO>.Fail("Invalid article id"));

        var response = await _helpService.GetArticleEngagement(
            articleId, ResolveVoterKey());

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    private string? ResolveVoterKey()
    {
        var userId = GetUserId();
        return userId == Guid.Empty ? null : userId.ToString();
    }

    private string ResolveAuthorName()
    {
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
        if (!string.IsNullOrWhiteSpace(email))
            return email.Trim();

        return "Support Agent";
    }
}
