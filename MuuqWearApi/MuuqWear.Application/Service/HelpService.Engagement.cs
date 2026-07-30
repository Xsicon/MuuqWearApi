using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using HelpArticleModel = MuuqWear.Model.Models.HelpArticle.HelpArticle;
using HelpArticleCommentModel = MuuqWear.Model.Models.HelpArticle.HelpArticleComment;
using HelpArticleVoteModel = MuuqWear.Model.Models.HelpArticle.HelpArticleVote;

namespace MuuqWear.Application.Service;

public partial class HelpService
{
    // =============================================
    // ADD ARTICLE COMMENT (ADMIN)
    // =============================================
    public async Task<Response<HelpArticleCommentDTO>> AddArticleComment(
        Guid articleId, Guid? authorId, string authorName, string body)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<HelpArticleCommentDTO>.Fail("Invalid article id");

            if (string.IsNullOrWhiteSpace(body))
                return Response<HelpArticleCommentDTO>.Fail("Comment body is required");

            if (string.IsNullOrWhiteSpace(authorName))
                authorName = "Support Agent";

            var article = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Limit(1)
                .Get();

            if (article.Models.FirstOrDefault() == null)
                return Response<HelpArticleCommentDTO>.Fail("Article not found");

            var now = DateTime.UtcNow;
            var comment = new HelpArticleCommentModel
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                AuthorId = authorId is { } id && id != Guid.Empty ? id : null,
                AuthorName = authorName.Trim(),
                Body = body.Trim(),
                CreatedAt = now
            };

            var result = await _client.From<HelpArticleCommentModel>().Insert(comment);
            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<HelpArticleCommentDTO>.Fail("Failed to add comment");

            return Response<HelpArticleCommentDTO>.SuccessResponse(
                MapCommentToDto(inserted),
                "Comment added");
        }
        catch (Exception)
        {
            return Response<HelpArticleCommentDTO>.Fail("Unable to add comment.");
        }
    }

    // =============================================
    // SET ARTICLE VOTE (ADMIN) — upsert
    // =============================================
    public async Task<Response<HelpArticleEngagementDTO>> SetArticleVote(
        Guid articleId, string voterKey, string vote)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<HelpArticleEngagementDTO>.Fail("Invalid article id");

            if (string.IsNullOrWhiteSpace(voterKey))
                return Response<HelpArticleEngagementDTO>.Fail("Voter key is required");

            var normalized = HelpArticleVoteValue.Normalize(vote);
            if (normalized == null)
                return Response<HelpArticleEngagementDTO>
                    .Fail("Vote must be 'like' or 'dislike'");

            var articleResult = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Limit(1)
                .Get();

            if (articleResult.Models.FirstOrDefault() == null)
                return Response<HelpArticleEngagementDTO>.Fail("Article not found");

            // Upsert avoids lost races on (article_id, voter_key).
            await _client.From<HelpArticleVoteModel>().Upsert(new HelpArticleVoteModel
            {
                ArticleId = articleId,
                VoterKey = voterKey,
                Vote = normalized,
                CreatedAt = DateTime.UtcNow
            });

            return await GetArticleEngagement(articleId, voterKey);
        }
        catch (Exception)
        {
            return Response<HelpArticleEngagementDTO>.Fail("Unable to save vote.");
        }
    }

    // =============================================
    // GET ARTICLE ENGAGEMENT (ADMIN)
    // =============================================
    public async Task<Response<HelpArticleEngagementDTO>> GetArticleEngagement(
        Guid articleId, string? voterKey = null)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<HelpArticleEngagementDTO>.Fail("Invalid article id");

            var articleResult = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Limit(1)
                .Get();

            if (articleResult.Models.FirstOrDefault() == null)
                return Response<HelpArticleEngagementDTO>.Fail("Article not found");

            var comments = await GetCommentsForArticle(articleId);
            var (likeCount, dislikeCount, myVote) =
                await GetVoteStats(articleId, voterKey);

            return Response<HelpArticleEngagementDTO>.SuccessResponse(
                new HelpArticleEngagementDTO
                {
                    LikeCount = likeCount,
                    DislikeCount = dislikeCount,
                    MyVote = myVote,
                    Comments = comments
                },
                "Engagement fetched");
        }
        catch (Exception)
        {
            return Response<HelpArticleEngagementDTO>.Fail("Unable to load engagement.");
        }
    }

    private async Task EnrichAdminArticleAsync(HelpArticleDTO dto, string? voterKey)
    {
        dto.Comments = await GetCommentsForArticle(dto.Id);
        var (likeCount, dislikeCount, myVote) = await GetVoteStats(dto.Id, voterKey);
        dto.LikeCount = likeCount;
        dto.DislikeCount = dislikeCount;
        dto.MyVote = myVote;
    }

    private async Task<List<HelpArticleCommentDTO>> GetCommentsForArticle(Guid articleId)
    {
        var result = await _client
            .From<HelpArticleCommentModel>()
            .Filter("article_id",
                Supabase.Postgrest.Constants.Operator.Equals,
                articleId.ToString())
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        return result.Models.Select(MapCommentToDto).ToList();
    }

    private async Task<(int LikeCount, int DislikeCount, string? MyVote)> GetVoteStats(
        Guid articleId, string? voterKey)
    {
        var result = await _client
            .From<HelpArticleVoteModel>()
            .Filter("article_id",
                Supabase.Postgrest.Constants.Operator.Equals,
                articleId.ToString())
            .Get();

        var votes = result.Models;
        var likeCount = votes.Count(v =>
            v.Vote.Equals(HelpArticleVoteValue.Like, StringComparison.OrdinalIgnoreCase));
        var dislikeCount = votes.Count(v =>
            v.Vote.Equals(HelpArticleVoteValue.Dislike, StringComparison.OrdinalIgnoreCase));

        string? myVote = null;
        if (!string.IsNullOrWhiteSpace(voterKey))
        {
            myVote = votes
                .FirstOrDefault(v => v.VoterKey == voterKey)
                ?.Vote;
        }

        return (likeCount, dislikeCount, myVote);
    }

    private static HelpArticleCommentDTO MapCommentToDto(HelpArticleCommentModel comment) =>
        new()
        {
            Id = comment.Id,
            AuthorName = comment.AuthorName,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt ?? DateTime.UtcNow
        };
}
