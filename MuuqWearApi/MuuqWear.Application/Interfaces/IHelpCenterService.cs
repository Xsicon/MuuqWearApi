using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using Microsoft.AspNetCore.Http;

namespace MuuqWear.Application.Interfaces;

public interface IHelpCenterService
{
    // ─── Public ───────────────────────────────────────────────
    Task<Response<SupportTicketDTO>> SubmitTicket(SubmitTicketDTO request);

    // ─── Admin ────────────────────────────────────────────────
    Task<Response<PaginatedResponse<SupportTicketDTO>>> GetAllTickets(
        string? status, int page, int pageSize);
    Task<Response<SupportTicketDTO>> GetTicketById(Guid ticketId);
    Task<Response<SupportTicketDTO>> UpdateTicketStatus(
        Guid ticketId, string status);
    Task<Response<SupportTicketDTO>> UpdateTicket(
        Guid ticketId, UpdateTicketDTO request);
    Task<Response<SupportTicketReplyDTO>> AddTicketReply(
        Guid ticketId, Guid? senderId, string senderName, string message);
    Task<Response<SupportTicketDTO>> AssignTicketToMe(
        Guid ticketId, Guid userId, string displayName);
    Task<Response<TicketStatsDTO>> GetStats();

    // ─── Help articles (public) ───────────────────────────────
    Task<Response<PaginatedResponse<HelpArticleDTO>>> GetPublishedArticles(
        string? category, string? search, int page, int pageSize);
    Task<Response<HelpArticleDTO>> GetPublishedArticleById(Guid articleId);

    // ─── Help articles (admin) ────────────────────────────────
    Task<Response<PaginatedResponse<HelpArticleDTO>>> GetAdminArticles(
        string? category, string? status, string? search, int page, int pageSize);
    Task<Response<HelpArticleDTO>> GetAdminArticleById(
        Guid articleId, string? voterKey = null);
    Task<Response<HelpArticleDTO>> CreateArticle(SaveHelpArticleDTO request);
    Task<Response<HelpArticleDTO>> UpdateArticle(
        Guid articleId, SaveHelpArticleDTO request);
    Task<Response<HelpArticleDTO>> UpdateArticleStatus(
        Guid articleId, string status);
    Task<Response<bool>> DeleteArticle(Guid articleId);
    Task<Response<string>> UploadImage(IFormFile file);

    // ─── Help article engagement (admin) ──────────────────────
    Task<Response<HelpArticleCommentDTO>> AddArticleComment(
        Guid articleId, Guid? authorId, string authorName, string body);
    Task<Response<HelpArticleEngagementDTO>> SetArticleVote(
        Guid articleId, string voterKey, string vote);
    Task<Response<HelpArticleEngagementDTO>> GetArticleEngagement(
        Guid articleId, string? voterKey = null);
}
