using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.HelpCenterDTO;
using Microsoft.AspNetCore.Http;
using HelpArticleModel = MuuqWear.Model.Models.HelpArticle.HelpArticle;
using HelpArticleStepModel = MuuqWear.Model.Models.HelpArticle.HelpArticleStep;

namespace MuuqWear.Application.Service;

public partial class HelpService
{
    // =============================================
    // GET PUBLISHED ARTICLES (PUBLIC)
    // =============================================
    public async Task<Response<PaginatedResponse<HelpArticleDTO>>> GetPublishedArticles(
        string? category, string? search, int page, int pageSize)
    {
        try
        {
            return await GetArticlesPage(
                category,
                HelpArticleStatus.Published,
                search,
                page,
                pageSize);
        }
        catch (Exception)
        {
            return Response<PaginatedResponse<HelpArticleDTO>>
                .Fail("Unable to load articles.");
        }
    }

    // =============================================
    // GET PUBLISHED ARTICLE BY ID (PUBLIC)
    // =============================================
    public async Task<Response<HelpArticleDTO>> GetPublishedArticleById(
        Guid articleId)
    {
        try
        {
            var article = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    HelpArticleStatus.Published)
                .Single();

            if (article == null)
                return Response<HelpArticleDTO>.Fail("Article not found");

            await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Set(a => a.ViewCount, article.ViewCount + 1)
                .Update();

            article.ViewCount += 1;
            var steps = await GetStepsForArticle(articleId);

            return Response<HelpArticleDTO>.SuccessResponse(
                MapArticleToDto(article, steps),
                "Article fetched");
        }
        catch (Exception)
        {
            return Response<HelpArticleDTO>.Fail("Unable to load article.");
        }
    }

    // =============================================
    // GET ADMIN ARTICLES
    // =============================================
    public async Task<Response<PaginatedResponse<HelpArticleDTO>>> GetAdminArticles(
        string? category, string? status, string? search, int page, int pageSize)
    {
        try
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(status) ||
                                   status.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? null
                : HelpArticleStatus.Normalize(status);

            return await GetArticlesPage(
                category,
                normalizedStatus,
                search,
                page,
                pageSize);
        }
        catch (Exception)
        {
            return Response<PaginatedResponse<HelpArticleDTO>>
                .Fail("Unable to load articles.");
        }
    }

    // =============================================
    // GET ADMIN ARTICLE BY ID
    // =============================================
    public async Task<Response<HelpArticleDTO>> GetAdminArticleById(
        Guid articleId, string? voterKey = null)
    {
        try
        {
            var article = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Single();

            if (article == null)
                return Response<HelpArticleDTO>.Fail("Article not found");

            var steps = await GetStepsForArticle(articleId);
            var dto = MapArticleToDto(article, steps);
            await EnrichAdminArticleAsync(dto, voterKey);

            return Response<HelpArticleDTO>.SuccessResponse(dto, "Article fetched");
        }
        catch (Exception)
        {
            return Response<HelpArticleDTO>.Fail("Unable to load article.");
        }
    }

    // =============================================
    // CREATE ARTICLE (ADMIN)
    // =============================================
    public async Task<Response<HelpArticleDTO>> CreateArticle(
        SaveHelpArticleDTO request)
    {
        try
        {
            var validation = ValidateSaveRequest(request);
            if (validation != null)
                return validation;

            var now = DateTime.UtcNow;
            var status = HelpArticleStatus.Normalize(request.Status);
            var article = new HelpArticleModel
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Category = request.Category.Trim(),
                Content = request.Content.Trim(),
                Status = status,
                HeroImageUrl = string.IsNullOrWhiteSpace(request.HeroImageUrl)
                    ? null
                    : request.HeroImageUrl.Trim(),
                ViewCount = 0,
                HelpfulCount = 0,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedAt = status == HelpArticleStatus.Published ? now : null
            };

            var result = await _client.From<HelpArticleModel>().Insert(article);
            var inserted = result.Models.FirstOrDefault();
            if (inserted == null)
                return Response<HelpArticleDTO>.Fail("Failed to create article");

            var steps = await ReplaceSteps(inserted.Id, request.Steps);

            return Response<HelpArticleDTO>.SuccessResponse(
                MapArticleToDto(inserted, steps),
                "Article created");
        }
        catch (Exception)
        {
            return Response<HelpArticleDTO>.Fail("Unable to create article.");
        }
    }

    // =============================================
    // UPDATE ARTICLE (ADMIN)
    // =============================================
    public async Task<Response<HelpArticleDTO>> UpdateArticle(
        Guid articleId, SaveHelpArticleDTO request)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<HelpArticleDTO>.Fail("Invalid article id");

            var validation = ValidateSaveRequest(request);
            if (validation != null)
                return validation;

            var existing = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Single();

            if (existing == null)
                return Response<HelpArticleDTO>.Fail("Article not found");

            var now = DateTime.UtcNow;
            var status = HelpArticleStatus.Normalize(request.Status);
            var publishedAt = existing.PublishedAt;
            if (status == HelpArticleStatus.Published && publishedAt == null)
                publishedAt = now;
            if (status == HelpArticleStatus.Draft)
                publishedAt = null;

            var result = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Set(a => a.Title, request.Title.Trim())
                .Set(a => a.Category, request.Category.Trim())
                .Set(a => a.Content, request.Content.Trim())
                .Set(a => a.Status, status)
                .Set(a => a.HeroImageUrl!,
                    string.IsNullOrWhiteSpace(request.HeroImageUrl)
                        ? null
                        : request.HeroImageUrl.Trim())
                .Set(a => a.UpdatedAt!, now)
                .Set(a => a.PublishedAt!, publishedAt)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<HelpArticleDTO>.Fail("Failed to update article");

            var steps = await ReplaceSteps(articleId, request.Steps);

            return Response<HelpArticleDTO>.SuccessResponse(
                MapArticleToDto(updated, steps),
                "Article updated");
        }
        catch (Exception)
        {
            return Response<HelpArticleDTO>.Fail("Unable to update article.");
        }
    }

    // =============================================
    // UPDATE ARTICLE STATUS (ADMIN)
    // =============================================
    public async Task<Response<HelpArticleDTO>> UpdateArticleStatus(
        Guid articleId, string status)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<HelpArticleDTO>.Fail("Invalid article id");

            var normalized = HelpArticleStatus.Normalize(status);
            if (!HelpArticleStatus.All.Contains(normalized))
                return Response<HelpArticleDTO>.Fail("Invalid status");

            var existing = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Single();

            if (existing == null)
                return Response<HelpArticleDTO>.Fail("Article not found");

            var now = DateTime.UtcNow;
            DateTime? publishedAt = normalized == HelpArticleStatus.Published
                ? existing.PublishedAt ?? now
                : null;

            var result = await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Set(a => a.Status, normalized)
                .Set(a => a.UpdatedAt!, now)
                .Set(a => a.PublishedAt!, publishedAt)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<HelpArticleDTO>.Fail("Failed to update status");

            var steps = await GetStepsForArticle(articleId);

            return Response<HelpArticleDTO>.SuccessResponse(
                MapArticleToDto(updated, steps),
                "Status updated");
        }
        catch (Exception)
        {
            return Response<HelpArticleDTO>.Fail("Unable to update article status.");
        }
    }

    // =============================================
    // DELETE ARTICLE (ADMIN)
    // =============================================
    public async Task<Response<bool>> DeleteArticle(Guid articleId)
    {
        try
        {
            if (articleId == Guid.Empty)
                return Response<bool>.Fail("Invalid article id");

            await _client
                .From<HelpArticleStepModel>()
                .Filter("article_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Delete();

            await _client
                .From<HelpArticleModel>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Delete();

            return Response<bool>.SuccessResponse(true, "Article deleted");
        }
        catch (Exception)
        {
            return Response<bool>.Fail("Unable to delete article.");
        }
    }

    private async Task<Response<PaginatedResponse<HelpArticleDTO>>> GetArticlesPage(
        string? category,
        string? status,
        string? search,
        int page,
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var offset = (page - 1) * pageSize;
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var canonicalCategory = ResolveCategoryFilter(category);

        if (!string.IsNullOrWhiteSpace(category)
            && !category.Equals("All", StringComparison.OrdinalIgnoreCase)
            && canonicalCategory == null)
        {
            return Response<PaginatedResponse<HelpArticleDTO>>.SuccessResponse(
                new PaginatedResponse<HelpArticleDTO>
                {
                    Data = [],
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 0,
                    HasMore = false
                },
                "Articles fetched");
        }

        // Push status/category to PostgREST; paginate server-side when not searching.
        var query = _client
            .From<HelpArticleModel>()
            .Order("updated_at", Supabase.Postgrest.Constants.Ordering.Descending);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Filter("status",
                Supabase.Postgrest.Constants.Operator.Equals,
                status);
        }

        if (canonicalCategory != null)
        {
            query = query.Filter("category",
                Supabase.Postgrest.Constants.Operator.Equals,
                canonicalCategory);
        }

        if (!hasSearch)
            query = query.Range(offset, offset + pageSize - 1);

        var result = await query.Get();
        IEnumerable<HelpArticleModel> filtered = result.Models ?? [];

        if (hasSearch)
        {
            var term = search!.Trim();
            filtered = filtered.Where(a =>
                a.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Content.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = filtered.ToList();
        int totalCount;
        List<HelpArticleModel> pageItems;

        if (hasSearch)
        {
            // Search still filters in-memory within the status/category result set.
            totalCount = materialized.Count;
            pageItems = materialized.Skip(offset).Take(pageSize).ToList();
        }
        else
        {
            // Range already applied; approximate total from page fullness when count RPC absent.
            pageItems = materialized;
            totalCount = pageItems.Count < pageSize
                ? offset + pageItems.Count
                : offset + pageItems.Count + 1;
        }

        var items = pageItems
            .Select(article => MapArticleToDto(article, []))
            .ToList();

        var totalPages = pageSize == 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        if (!hasSearch && pageItems.Count == pageSize)
            totalPages = Math.Max(totalPages, page + 1);

        var paginated = new PaginatedResponse<HelpArticleDTO>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasMore = page < totalPages || (!hasSearch && pageItems.Count == pageSize)
        };

        return Response<PaginatedResponse<HelpArticleDTO>>
            .SuccessResponse(paginated, "Articles fetched");
    }

    private async Task<List<HelpArticleStepModel>> GetStepsForArticle(Guid articleId)
    {
        var result = await _client
            .From<HelpArticleStepModel>()
            .Filter("article_id",
                Supabase.Postgrest.Constants.Operator.Equals,
                articleId.ToString())
            .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return result.Models ?? [];
    }

    private async Task<List<HelpArticleStepModel>> ReplaceSteps(
        Guid articleId, List<HelpArticleStepDTO>? steps)
    {
        steps ??= [];

        var existing = await GetStepsForArticle(articleId);
        var existingIds = existing.Select(s => s.Id).ToList();

        if (steps.Count == 0)
        {
            if (existingIds.Count > 0)
            {
                await _client
                    .From<HelpArticleStepModel>()
                    .Filter("article_id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        articleId.ToString())
                    .Delete();
            }

            return [];
        }

        // Insert new rows first so a failed insert does not wipe existing steps.
        var rows = steps
            .Select((step, index) => new HelpArticleStepModel
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                SortOrder = step.SortOrder >= 0 ? step.SortOrder : index,
                Detail = (step.Detail ?? string.Empty).Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(step.ImageUrl)
                    ? null
                    : step.ImageUrl.Trim()
            })
            .ToList();

        var inserted = await _client.From<HelpArticleStepModel>().Insert(rows);
        var insertedModels = inserted.Models ?? [];
        if (insertedModels.Count == 0)
            throw new InvalidOperationException("Failed to insert article steps.");

        if (existingIds.Count > 0)
        {
            await _client
                .From<HelpArticleStepModel>()
                .Filter("article_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    articleId.ToString())
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.In,
                    existingIds.Select(id => id.ToString()).ToList())
                .Delete();
        }

        return insertedModels
            .OrderBy(s => s.SortOrder)
            .ToList();
    }

    private static string? ResolveCategoryFilter(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return HelpArticleCategory.All.FirstOrDefault(c =>
            c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static Response<HelpArticleDTO>? ValidateSaveRequest(SaveHelpArticleDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Response<HelpArticleDTO>.Fail("Title is required");

        if (string.IsNullOrWhiteSpace(request.Category))
            return Response<HelpArticleDTO>.Fail("Category is required");

        if (!HelpArticleCategory.All.Contains(
                request.Category.Trim(), StringComparer.OrdinalIgnoreCase))
            return Response<HelpArticleDTO>.Fail("Invalid category");

        // Normalize to canonical category casing.
        request.Category = HelpArticleCategory.All.First(c =>
            c.Equals(request.Category.Trim(), StringComparison.OrdinalIgnoreCase));

        return null;
    }

    private static HelpArticleDTO MapArticleToDto(
        HelpArticleModel article, List<HelpArticleStepModel> steps) =>
        new()
        {
            Id = article.Id,
            Title = article.Title,
            Category = article.Category,
            Content = article.Content,
            Status = article.Status,
            HeroImageUrl = article.HeroImageUrl,
            ViewCount = article.ViewCount,
            HelpfulCount = article.HelpfulCount,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            PublishedAt = article.PublishedAt,
            Steps = steps.Select(s => new HelpArticleStepDTO
            {
                Id = s.Id,
                SortOrder = s.SortOrder,
                Detail = s.Detail,
                ImageUrl = s.ImageUrl
            }).ToList()
        };

    // =============================================
    // UPLOAD IMAGE (ADMIN)
    // =============================================
    public async Task<Response<string>> UploadImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return Response<string>.Fail("No file provided");

            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
                return Response<string>.Fail("Image must be 5 MB or smaller");

            if (string.IsNullOrWhiteSpace(file.ContentType)
                || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return Response<string>.Fail("File must be an image");
            }

            using var stream = file.OpenReadStream();
            var buffer = new byte[file.Length];
            await stream.ReadAsync(buffer);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var fileName = $"help/{Guid.NewGuid()}{extension}";

            await _client.Storage
                .From("app-images")
                .Upload(buffer, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                });

            var publicUrl = _client.Storage
                .From("app-images")
                .GetPublicUrl(fileName);

            return Response<string>.SuccessResponse(publicUrl, "Image uploaded successfully");
        }
        catch (Exception)
        {
            return Response<string>.Fail("Unable to upload image.");
        }
    }
}
