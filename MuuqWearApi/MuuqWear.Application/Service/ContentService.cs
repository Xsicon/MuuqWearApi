using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.ContentItemDTO;
using MuuqWear.Model.Models.DesignHistory;
using MuuqWear.Model.Models.Event;
using MuuqWear.Model.Models.JournalArticle;

namespace MuuqWear.API.Service;

public class ContentService : IContentService
{
    private static readonly HashSet<string> AllowedJournalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Culture", "Design", "Innovation", "Lifestyle", "Tech"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "draft", "published", "scheduled", "archived"
    };

    private const int MaxTags = 20;
    private const int MaxTagLength = 50;
    private const int WordsPerMinute = 200;

    private readonly Supabase.Client _client;

    public ContentService(SupabaseAdminClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    public async Task<Response<List<ContentItemDTO>>> GetAll(ContentCategory type)
    {
        try
        {
            if (type == ContentCategory.JournalArticles)
                await PromoteDueScheduledArticlesAsync();

            var items = type switch
            {
                ContentCategory.JournalArticles =>
                    (await _client.From<JournalArticle>()
                        .Order("created_at",
                            Supabase.Postgrest.Constants.Ordering.Descending)
                        .Get()).Models.Select(MapJournalArticleToDto).ToList(),

                ContentCategory.Events =>
                    (await _client.From<Event>()
                        .Order("created_at",
                            Supabase.Postgrest.Constants.Ordering.Descending)
                        .Get()).Models.Select(x => new ContentItemDTO
                        {
                            Id = x.Id,
                            Title = x.Title,
                            Content = x.Content,
                            Status = x.Status,
                            Views = x.Views,
                            CreatedAt = x.CreatedAt,
                            PublishedAt = x.PublishedAt
                        }).ToList(),

                ContentCategory.DesignHistory =>
                    (await _client.From<DesignHistory>()
                        .Order("created_at",
                            Supabase.Postgrest.Constants.Ordering.Descending)
                        .Get()).Models.Select(MapDesignHistoryToDto).ToList(),

                _ => new List<ContentItemDTO>()
            };

            return Response<List<ContentItemDTO>>.SuccessResponse(
                items, "Content fetched");
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> GetById(ContentCategory type, Guid id)
    {
        try
        {
            if (type == ContentCategory.JournalArticles)
                await PromoteDueScheduledArticlesAsync();

            ContentItemDTO? item = type switch
            {
                ContentCategory.JournalArticles => await TryGetJournalDtoByIdAsync(id),

                ContentCategory.Events => await _client
                    .From<Event>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Single() is { } e ? new ContentItemDTO
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Content = e.Content,
                        Status = e.Status,
                        Views = e.Views,
                        CreatedAt = e.CreatedAt,
                        PublishedAt = e.PublishedAt
                    } : null,

                ContentCategory.DesignHistory => await _client
                    .From<DesignHistory>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Single() is { } d ? MapDesignHistoryToDto(d) : null,

                _ => null
            };

            if (item == null)
                return Response<ContentItemDTO>.Fail("Item not found");

            return Response<ContentItemDTO>.SuccessResponse(item, "Item fetched");
        }
        catch (Exception ex)
        {
            if (LooksLikeNotFound(ex.Message))
                return Response<ContentItemDTO>.Fail("Item not found");

            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> Create(
        ContentCategory type, CreateContentItemDTO request)
    {
        try
        {
            ContentItemDTO? created = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                {
                    var validationError = ValidateJournalRequest(
                        request.Title,
                        request.Category,
                        request.Status,
                        request.ScheduledAt,
                        request.IsFeatured,
                        request.Tags,
                        requireStatusInAllowedSet: false);
                    if (validationError != null)
                        return Response<ContentItemDTO>.Fail(validationError);

                    var status = NormalizeStatus(request.Status) ?? "draft";
                    var tags = NormalizeTags(request.Tags);
                    var slug = await ResolveUniqueSlugAsync(
                        request.Slug, request.Title, excludeId: null);
                    if (slug.StartsWith("ERROR:", StringComparison.Ordinal))
                        return Response<ContentItemDTO>.Fail(slug["ERROR:".Length..].Trim());

                    if (request.IsFeatured == true && status != "published")
                        return Response<ContentItemDTO>.Fail(
                            "Only published articles may be featured");

                    if (request.IsFeatured == true)
                        await ClearFeaturedArticlesAsync(exceptId: null);

                    var readTime = ResolveReadTimeMinutes(
                        request.ReadTimeMinutes, request.Content);

                    DateTime? publishedAt = status == "published"
                        ? DateTime.UtcNow
                        : null;

                    var ja = (await _client.From<JournalArticle>()
                        .Insert(new JournalArticle
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Title.Trim(),
                            Content = request.Content,
                            Status = status,
                            CreatedAt = DateTime.UtcNow,
                            PublishedAt = publishedAt,
                            Category = NormalizeCategory(request.Category),
                            ImageUrl = request.ImageUrl,
                            Author = NullIfWhiteSpace(request.Author),
                            Excerpt = NullIfWhiteSpace(request.Excerpt),
                            Slug = slug,
                            SeoTitle = NullIfWhiteSpace(request.SeoTitle),
                            Tags = tags,
                            IsFeatured = request.IsFeatured == true,
                            ScheduledAt = status == "scheduled"
                                ? request.ScheduledAt
                                : null,
                            ReadTimeMinutes = readTime
                        })).Models.FirstOrDefault();

                    if (ja != null)
                        created = MapJournalArticleToDto(ja);
                    break;
                }

                case ContentCategory.Events:
                    var ev = (await _client.From<Event>()
                        .Insert(new Event
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Title,
                            Content = request.Content,
                            Status = "draft",
                            CreatedAt = DateTime.UtcNow
                        })).Models.FirstOrDefault();
                    if (ev != null)
                        created = new ContentItemDTO
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Content = ev.Content,
                            Status = ev.Status,
                            Views = ev.Views,
                            CreatedAt = ev.CreatedAt,
                            PublishedAt = ev.PublishedAt
                        };
                    break;

                case ContentCategory.DesignHistory:
                    var dh = (await _client.From<DesignHistory>()
                        .Insert(new DesignHistory
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Title,
                            Content = request.Content,
                            Status = "draft",
                            CreatedAt = DateTime.UtcNow,
                            Designer = request.Designer,
                            Year = request.Year,
                            Inspiration = request.Inspiration,
                            Collection = request.Collection,
                            SecondImageUrl = request.SecondImageUrl,
                            TechnicalFabric = request.TechnicalFabric,
                            TechnicalTechniques = request.TechnicalTechniques,
                            TechnicalProduction = request.TechnicalProduction,
                            TechnicalAvailability = request.TechnicalAvailability,
                            ImageUrl = request.ImageUrl,
                            ProductId = request.ProductId
                        })).Models.FirstOrDefault();
                    if (dh != null)
                        created = MapDesignHistoryToDto(dh);
                    break;
            }

            if (created == null)
                return Response<ContentItemDTO>.Fail("Failed to create item");

            return Response<ContentItemDTO>.SuccessResponse(created, "Item created");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> Update(
        ContentCategory type, Guid id, UpdateContentItemDTO request)
    {
        try
        {
            ContentItemDTO? updated = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                {
                    JournalArticle? existing;
                    try
                    {
                        existing = await _client.From<JournalArticle>()
                            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                                id.ToString())
                            .Single();
                    }
                    catch
                    {
                        return Response<ContentItemDTO>.Fail("Item not found");
                    }

                    if (existing == null)
                        return Response<ContentItemDTO>.Fail("Item not found");

                    var status = NormalizeStatus(request.Status) ?? existing.Status;
                    var effectiveScheduledAt = status == "scheduled"
                        ? (request.ScheduledAt ?? existing.ScheduledAt)
                        : null;

                    var validationError = ValidateJournalRequest(
                        request.Title,
                        request.Category,
                        status,
                        effectiveScheduledAt,
                        request.IsFeatured ?? existing.IsFeatured,
                        request.Tags,
                        requireStatusInAllowedSet: true);
                    if (validationError != null)
                        return Response<ContentItemDTO>.Fail(validationError);

                    var tags = request.Tags != null
                        ? NormalizeTags(request.Tags)
                        : existing.Tags ?? new List<string>();

                    var slugSource = !string.IsNullOrWhiteSpace(request.Slug)
                        ? request.Slug
                        : existing.Slug;
                    var slug = await ResolveUniqueSlugAsync(
                        slugSource, request.Title, excludeId: id);
                    if (slug.StartsWith("ERROR:", StringComparison.Ordinal))
                        return Response<ContentItemDTO>.Fail(slug["ERROR:".Length..].Trim());

                    var isFeatured = request.IsFeatured ?? existing.IsFeatured;
                    if (status != "published")
                        isFeatured = false;

                    if (isFeatured)
                        await ClearFeaturedArticlesAsync(exceptId: id);

                    var readTime = ResolveReadTimeMinutes(
                        request.ReadTimeMinutes, request.Content);

                    DateTime? publishedAt = existing.PublishedAt;
                    if (status == "published" && existing.Status != "published")
                        publishedAt = DateTime.UtcNow;
                    else if (status != "published")
                        publishedAt = status == "scheduled" ? null : existing.PublishedAt;

                    var ja = (await _client.From<JournalArticle>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Title, request.Title.Trim())
                        .Set(x => x.Content!, request.Content)
                        .Set(x => x.Status, status)
                        .Set(x => x.Category!, NormalizeCategory(request.Category))
                        .Set(x => x.ImageUrl!, request.ImageUrl)
                        .Set(x => x.Author!, NullIfWhiteSpace(request.Author))
                        .Set(x => x.Excerpt!, NullIfWhiteSpace(request.Excerpt))
                        .Set(x => x.Slug!, slug)
                        .Set(x => x.SeoTitle!, NullIfWhiteSpace(request.SeoTitle))
                        .Set(x => x.Tags!, tags)
                        .Set(x => x.IsFeatured, isFeatured)
                        .Set(x => x.ScheduledAt!, effectiveScheduledAt)
                        .Set(x => x.ReadTimeMinutes!, readTime)
                        .Set(x => x.PublishedAt!, publishedAt)
                        .Update()).Models.FirstOrDefault();

                    if (ja != null)
                        updated = MapJournalArticleToDto(ja);
                    break;
                }

                case ContentCategory.Events:
                    var ev = (await _client.From<Event>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Title, request.Title)
                        .Set(x => x.Content!, request.Content)
                        .Update()).Models.FirstOrDefault();
                    if (ev != null)
                        updated = new ContentItemDTO
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Content = ev.Content,
                            Status = ev.Status,
                            Views = ev.Views,
                            CreatedAt = ev.CreatedAt,
                            PublishedAt = ev.PublishedAt
                        };
                    break;

                case ContentCategory.DesignHistory:
                    var dh = (await _client.From<DesignHistory>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Title, request.Title)
                        .Set(x => x.Content!, request.Content)
                        .Set(x => x.Designer!, request.Designer)
                        .Set(x => x.Year!, request.Year)
                        .Set(x => x.Inspiration!, request.Inspiration)
                        .Set(x => x.Collection!, request.Collection)
                        .Set(x => x.SecondImageUrl!, request.SecondImageUrl)
                        .Set(x => x.ImageUrl!, request.ImageUrl)
                        .Set(x => x.TechnicalFabric!, request.TechnicalFabric)
                        .Set(x => x.TechnicalTechniques!, request.TechnicalTechniques)
                        .Set(x => x.TechnicalProduction!, request.TechnicalProduction)
                        .Set(x => x.TechnicalAvailability!, request.TechnicalAvailability)
                        .Set(x => x.ProductId!, request.ProductId)
                        .Update()).Models.FirstOrDefault();
                    if (dh != null)
                        updated = MapDesignHistoryToDto(dh);
                    break;
            }

            if (updated == null)
                return Response<ContentItemDTO>.Fail("Failed to update item");

            return Response<ContentItemDTO>.SuccessResponse(updated, "Item updated");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<bool>> Delete(ContentCategory type, Guid id)
    {
        try
        {
            switch (type)
            {
                case ContentCategory.JournalArticles:
                    await _client.From<JournalArticle>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Delete();
                    break;

                case ContentCategory.Events:
                    await _client.From<Event>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Delete();
                    break;

                case ContentCategory.DesignHistory:
                    await _client.From<DesignHistory>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Delete();
                    break;
            }

            return Response<bool>.SuccessResponse(true, "Item deleted");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> Publish(ContentCategory type, Guid id)
    {
        try
        {
            ContentItemDTO? published = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                    var ja = (await _client.From<JournalArticle>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "published")
                        .Set(x => x.PublishedAt!, DateTime.UtcNow)
                        .Set(x => x.ScheduledAt!, (DateTime?)null)
                        .Update()).Models.FirstOrDefault();
                    if (ja != null)
                        published = MapJournalArticleToDto(ja);
                    break;

                case ContentCategory.Events:
                    var ev = (await _client.From<Event>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "published")
                        .Set(x => x.PublishedAt!, DateTime.UtcNow)
                        .Update()).Models.FirstOrDefault();
                    if (ev != null)
                        published = new ContentItemDTO
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Content = ev.Content,
                            Status = ev.Status,
                            Views = ev.Views,
                            CreatedAt = ev.CreatedAt,
                            PublishedAt = ev.PublishedAt
                        };
                    break;

                case ContentCategory.DesignHistory:
                    var dh = (await _client.From<DesignHistory>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "published")
                        .Set(x => x.PublishedAt!, DateTime.UtcNow)
                        .Update()).Models.FirstOrDefault();
                    if (dh != null)
                        published = MapDesignHistoryToDto(dh);
                    break;
            }

            if (published == null)
                return Response<ContentItemDTO>.Fail("Failed to publish item");

            return Response<ContentItemDTO>.SuccessResponse(published, "Item published");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> Unpublish(ContentCategory type, Guid id)
    {
        try
        {
            ContentItemDTO? unpublished = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                    var ja = (await _client.From<JournalArticle>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "draft")
                        .Set(x => x.PublishedAt!, (DateTime?)null)
                        .Set(x => x.IsFeatured, false)
                        .Set(x => x.ScheduledAt!, (DateTime?)null)
                        .Update()).Models.FirstOrDefault();
                    if (ja != null)
                        unpublished = MapJournalArticleToDto(ja);
                    break;

                case ContentCategory.Events:
                    var ev = (await _client.From<Event>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "draft")
                        .Set(x => x.PublishedAt!, (DateTime?)null)
                        .Update()).Models.FirstOrDefault();
                    if (ev != null)
                        unpublished = new ContentItemDTO
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Content = ev.Content,
                            Status = ev.Status,
                            Views = ev.Views,
                            CreatedAt = ev.CreatedAt,
                            PublishedAt = ev.PublishedAt
                        };
                    break;

                case ContentCategory.DesignHistory:
                    var dh = (await _client.From<DesignHistory>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                            id.ToString())
                        .Set(x => x.Status, "draft")
                        .Set(x => x.PublishedAt!, (DateTime?)null)
                        .Update()).Models.FirstOrDefault();
                    if (dh != null)
                        unpublished = MapDesignHistoryToDto(dh);
                    break;
            }

            if (unpublished == null)
                return Response<ContentItemDTO>.Fail("Failed to unpublish item");

            return Response<ContentItemDTO>.SuccessResponse(
                unpublished, "Item unpublished");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<string>> UploadImage(IFormFile file)
    {
        try
        {
            var fileName =
                $"articles/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            await _client.Storage
                .From("app-images")
                .Upload(bytes, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                });

            var url = _client.Storage
                .From("app-images")
                .GetPublicUrl(fileName);

            return Response<string>.SuccessResponse(url, "Image uploaded");
        }
        catch (Exception ex)
        {
            return Response<string>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<PaginatedResponse<ContentItemDTO>>> GetPublished(
        int page, int pageSize, string? category = null)
    {
        try
        {
            await PromoteDueScheduledArticlesAsync();

            var offset = (page - 1) * pageSize;

            var totalCount = await CountPublishedArticles(category);
            var items = await FetchPublishedPage(offset, pageSize, category);
            var featured = await GetFeaturedPublishedArticleAsync();

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedResult = new PaginatedResponse<ContentItemDTO>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages,
                FeaturedArticle = featured
            };

            return Response<PaginatedResponse<ContentItemDTO>>.SuccessResponse(
                pagedResult, "Articles fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<ContentItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> GetFeaturedPublished()
    {
        try
        {
            await PromoteDueScheduledArticlesAsync();

            var featured = await GetFeaturedPublishedArticleAsync();
            if (featured == null)
                return Response<ContentItemDTO>.Fail("No featured article found");

            return Response<ContentItemDTO>.SuccessResponse(featured, "Featured article fetched");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<ContentItemDTO>> GetPublishedBySlug(string slug)
    {
        try
        {
            await PromoteDueScheduledArticlesAsync();

            if (string.IsNullOrWhiteSpace(slug))
                return Response<ContentItemDTO>.Fail("Slug is required");

            var result = await _client.From<JournalArticle>()
                .Filter("slug", Supabase.Postgrest.Constants.Operator.Equals, slug.Trim())
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Limit(1)
                .Get();

            var article = result.Models.FirstOrDefault();
            if (article == null)
                return Response<ContentItemDTO>.Fail("Article not found");

            return Response<ContentItemDTO>.SuccessResponse(
                MapJournalArticleToDto(article), "Article fetched");
        }
        catch (Exception ex)
        {
            if (LooksLikeNotFound(ex.Message))
                return Response<ContentItemDTO>.Fail("Article not found");

            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<int>> RecordJournalView(Guid id)
    {
        try
        {
            var existingResult = await _client.From<JournalArticle>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Limit(1)
                .Get();

            var existing = existingResult.Models.FirstOrDefault();
            if (existing == null)
                return Response<int>.Fail("Article not found");

            var updated = (await _client.From<JournalArticle>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.Views, existing.Views + 1)
                .Update()).Models.FirstOrDefault();

            if (updated == null)
                return Response<int>.Fail("Failed to record view");

            return Response<int>.SuccessResponse(updated.Views, "View recorded");
        }
        catch (Exception ex)
        {
            if (LooksLikeNotFound(ex.Message))
                return Response<int>.Fail("Article not found");

            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    private async Task<int> CountPublishedArticles(string? category)
    {
        var query = _client.From<JournalArticle>()
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals,
                "published");

        if (!string.IsNullOrEmpty(category))
            query = query.Filter("category",
                Supabase.Postgrest.Constants.Operator.Equals, category);

        try
        {
            return await query.Count(Supabase.Postgrest.Constants.CountType.Exact);
        }
        catch
        {
            var fallbackQuery = _client.From<JournalArticle>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals,
                    "published");

            if (!string.IsNullOrEmpty(category))
                fallbackQuery = fallbackQuery.Filter("category",
                    Supabase.Postgrest.Constants.Operator.Equals, category);

            var rows = await fallbackQuery.Get();
            return rows.Models.Count;
        }
    }

    private async Task<List<ContentItemDTO>> FetchPublishedPage(
        int offset, int pageSize, string? category)
    {
        var query = _client.From<JournalArticle>()
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals,
                "published");

        if (!string.IsNullOrEmpty(category))
            query = query.Filter("category",
                Supabase.Postgrest.Constants.Operator.Equals, category);

        var result = await query
            .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return result.Models
            .Where(x => x.Status.Equals("published", StringComparison.OrdinalIgnoreCase))
            .Select(MapJournalArticleListDto)
            .ToList();
    }

    private async Task<ContentItemDTO?> GetFeaturedPublishedArticleAsync()
    {
        try
        {
            var featured = await _client.From<JournalArticle>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Filter("is_featured", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            var article = featured.Models.FirstOrDefault();
            return article == null ? null : MapJournalArticleListDto(article);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Response<List<ContentItemDTO>>> GetPublishedDesignHistory()
    {
        try
        {
            var result = await _client.From<DesignHistory>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals,
                    "published")
                .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var items = result.Models.Select(MapDesignHistoryToDto).ToList();

            return Response<List<ContentItemDTO>>.SuccessResponse(
                items, "Design history fetched");
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<int>> RecordDesignHistoryView(Guid id)
    {
        try
        {
            var existing = await _client.From<DesignHistory>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Single();

            if (existing == null)
                return Response<int>.Fail("Design history item not found");

            var updated = (await _client.From<DesignHistory>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.Views, existing.Views + 1)
                .Update()).Models.FirstOrDefault();

            if (updated == null)
                return Response<int>.Fail("Failed to record view");

            return Response<int>.SuccessResponse(updated.Views, "View recorded");
        }
        catch (Exception ex)
        {
            return Response<int>.Fail("Error: " + ex.Message);
        }
    }

    private async Task PromoteDueScheduledArticlesAsync()
    {
        try
        {
            var due = await _client.From<JournalArticle>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "scheduled")
                .Filter("scheduled_at", Supabase.Postgrest.Constants.Operator.LessThanOrEqual,
                    DateTime.UtcNow.ToString("o"))
                .Get();

            foreach (var article in due.Models)
            {
                await _client.From<JournalArticle>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                        article.Id.ToString())
                    .Set(x => x.Status, "published")
                    .Set(x => x.PublishedAt!, DateTime.UtcNow)
                    .Set(x => x.ScheduledAt!, (DateTime?)null)
                    .Update();
            }
        }
        catch
        {
            // Best-effort promotion on read
        }
    }

    private async Task ClearFeaturedArticlesAsync(Guid? exceptId)
    {
        var featured = await _client.From<JournalArticle>()
            .Filter("is_featured", Supabase.Postgrest.Constants.Operator.Equals, "true")
            .Get();

        foreach (var article in featured.Models)
        {
            if (exceptId.HasValue && article.Id == exceptId.Value)
                continue;

            await _client.From<JournalArticle>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    article.Id.ToString())
                .Set(x => x.IsFeatured, false)
                .Update();
        }
    }

    private async Task<string> ResolveUniqueSlugAsync(
        string? requestedSlug, string title, Guid? excludeId)
    {
        var baseSlug = string.IsNullOrWhiteSpace(requestedSlug)
            ? GenerateSlug(title)
            : GenerateSlug(requestedSlug);

        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "article";

        var candidate = baseSlug;
        var suffix = 2;

        while (await SlugExistsAsync(candidate, excludeId))
        {
            if (!string.IsNullOrWhiteSpace(requestedSlug) && candidate == baseSlug)
                return $"ERROR:Slug '{baseSlug}' is already in use";

            candidate = $"{baseSlug}-{suffix}";
            suffix++;
            if (suffix > 100)
                return "ERROR:Unable to generate a unique slug";
        }

        return candidate;
    }

    private async Task<bool> SlugExistsAsync(string slug, Guid? excludeId)
    {
        var result = await _client.From<JournalArticle>()
            .Filter("slug", Supabase.Postgrest.Constants.Operator.Equals, slug)
            .Get();

        return result.Models.Any(x => !excludeId.HasValue || x.Id != excludeId.Value);
    }

    private static string? ValidateJournalRequest(
        string title,
        string? category,
        string? status,
        DateTime? scheduledAt,
        bool? isFeatured,
        List<string>? tags,
        bool requireStatusInAllowedSet = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";

        if (!string.IsNullOrWhiteSpace(category)
            && !AllowedJournalCategories.Contains(category.Trim()))
        {
            return "Category must be one of: Culture, Design, Innovation, Lifestyle, Tech";
        }

        var normalizedStatus = NormalizeStatus(status);
        if (requireStatusInAllowedSet)
        {
            if (normalizedStatus == null)
                return "Status must be one of: draft, published, scheduled, archived";
        }
        else if (status != null && normalizedStatus == null)
        {
            return "Status must be one of: draft, published, scheduled, archived";
        }

        var effectiveStatus = normalizedStatus ?? status?.Trim().ToLowerInvariant();

        if (effectiveStatus == "scheduled")
        {
            if (!scheduledAt.HasValue)
                return "scheduled_at is required when status is scheduled";

            if (scheduledAt.Value.ToUniversalTime() <= DateTime.UtcNow)
                return "scheduled_at must be in the future";
        }

        if (isFeatured == true
            && !string.IsNullOrWhiteSpace(effectiveStatus)
            && effectiveStatus != "published")
        {
            return "Only published articles may be featured";
        }

        if (tags != null)
        {
            if (tags.Count > MaxTags)
                return $"A maximum of {MaxTags} tags is allowed";

            if (tags.Any(t => (t?.Trim().Length ?? 0) > MaxTagLength))
                return $"Each tag must be {MaxTagLength} characters or fewer";
        }

        return null;
    }

    private async Task<ContentItemDTO?> TryGetJournalDtoByIdAsync(Guid id)
    {
        try
        {
            var result = await _client.From<JournalArticle>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Limit(1)
                .Get();

            var article = result.Models.FirstOrDefault();
            return article == null ? null : MapJournalArticleToDto(article);
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeNotFound(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("0 rows", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no rows", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Sequence contains no elements", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var trimmed = status.Trim().ToLowerInvariant();
        return AllowedStatuses.Contains(trimmed) ? trimmed : null;
    }

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        return AllowedJournalCategories.FirstOrDefault(c =>
            c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? category.Trim();
    }

    private static List<string> NormalizeTags(List<string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return new List<string>();

        return tags
            .SelectMany(t => (t ?? string.Empty).Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTags)
            .Select(t => t.Length > MaxTagLength ? t[..MaxTagLength] : t)
            .ToList();
    }

    private static int ResolveReadTimeMinutes(int? requested, string? content)
    {
        if (requested.HasValue && requested.Value >= 1)
            return requested.Value;

        return ComputeReadTimeMinutes(content);
    }

    private static int ComputeReadTimeMinutes(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 1;

        var plain = Regex.Replace(content, "<[^>]+>", " ");
        var words = Regex.Matches(plain, @"\b[\w']+\b").Count;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_')
                sb.Append('-');
        }

        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return slug;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContentItemDTO MapJournalArticleToDto(JournalArticle item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Content = item.Content,
        Status = item.Status,
        Views = item.Views,
        CreatedAt = item.CreatedAt,
        PublishedAt = item.PublishedAt,
        Category = item.Category,
        ImageUrl = item.ImageUrl,
        Author = item.Author,
        Excerpt = item.Excerpt,
        Slug = item.Slug,
        SeoTitle = item.SeoTitle,
        Tags = item.Tags ?? new List<string>(),
        IsFeatured = item.IsFeatured,
        ScheduledAt = item.ScheduledAt,
        ReadTimeMinutes = item.ReadTimeMinutes
    };

    private static ContentItemDTO MapJournalArticleListDto(JournalArticle item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Category = item.Category,
        ImageUrl = item.ImageUrl,
        Excerpt = item.Excerpt,
        Author = item.Author,
        ReadTimeMinutes = item.ReadTimeMinutes,
        PublishedAt = item.PublishedAt,
        Slug = item.Slug,
        Tags = item.Tags ?? new List<string>(),
        IsFeatured = item.IsFeatured,
        Status = item.Status,
        Views = item.Views,
        CreatedAt = item.CreatedAt,
        SeoTitle = item.SeoTitle,
        ScheduledAt = item.ScheduledAt
    };

    private static ContentItemDTO MapDesignHistoryToDto(DesignHistory item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Content = item.Content,
        Status = item.Status,
        Views = item.Views,
        CreatedAt = item.CreatedAt,
        PublishedAt = item.PublishedAt,
        Designer = item.Designer,
        Year = item.Year,
        Inspiration = item.Inspiration,
        Collection = item.Collection,
        SecondImageUrl = item.SecondImageUrl,
        TechnicalFabric = item.TechnicalFabric,
        TechnicalTechniques = item.TechnicalTechniques,
        TechnicalProduction = item.TechnicalProduction,
        TechnicalAvailability = item.TechnicalAvailability,
        ImageUrl = item.ImageUrl,
        ProductId = item.ProductId
    };
}
