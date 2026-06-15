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
    private readonly Supabase.Client _client;

    public ContentService(SupabaseAdminClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // GET ALL
    // =============================================
    public async Task<Response<List<ContentItemDTO>>> GetAll(ContentCategory type)
    {
        try
        {
            var items = type switch
            {
                ContentCategory.JournalArticles =>
                    (await _client.From<JournalArticle>()
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
                            PublishedAt = x.PublishedAt,
                            Category = x.Category,
                            ImageUrl = x.ImageUrl
                        }).ToList(),

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
                        .Get()).Models.Select(x => new ContentItemDTO
                        {
                            Id = x.Id,
                            Title = x.Title,
                            Content = x.Content,
                            Status = x.Status,
                            Views = x.Views,
                            CreatedAt = x.CreatedAt,
                            PublishedAt = x.PublishedAt,
                            Designer = x.Designer,
                            Year = x.Year,
                            Inspiration = x.Inspiration,
                            Collection = x.Collection,
                            SecondImageUrl = x.SecondImageUrl,
                            TechnicalFabric = x.TechnicalFabric,
                            TechnicalTechniques = x.TechnicalTechniques,
                            TechnicalProduction = x.TechnicalProduction,
                            TechnicalAvailability = x.TechnicalAvailability,
                            ImageUrl = x.ImageUrl


                        }).ToList(),

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

    // =============================================
    // GET BY ID
    // =============================================
    public async Task<Response<ContentItemDTO>> GetById(ContentCategory type, Guid id)
    {
        try
        {
            ContentItemDTO? item = type switch
            {
                ContentCategory.JournalArticles => await _client
                    .From<JournalArticle>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Single() is { } x ? new ContentItemDTO
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Content = x.Content,
                        Status = x.Status,
                        Views = x.Views,
                        CreatedAt = x.CreatedAt,
                        PublishedAt = x.PublishedAt,
                        Category = x.Category,
                        ImageUrl = x.ImageUrl
                    } : null,

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
                    .Single() is { } d ? new ContentItemDTO
                    {
                        Id = d.Id,
                        Title = d.Title,
                        Content = d.Content,
                        Status = d.Status,
                        Views = d.Views,
                        CreatedAt = d.CreatedAt,
                        PublishedAt = d.PublishedAt
                    } : null,

                _ => null
            };

            if (item == null)
                return Response<ContentItemDTO>.Fail("Item not found");

            return Response<ContentItemDTO>.SuccessResponse(item, "Item fetched");
        }
        catch (Exception ex)
        {
            return Response<ContentItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CREATE
    // =============================================
    public async Task<Response<ContentItemDTO>> Create(
        ContentCategory type, CreateContentItemDTO request)
    {
        try
        {
            ContentItemDTO? created = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                    var ja = (await _client.From<JournalArticle>()
                        .Insert(new JournalArticle
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Title,
                            Content = request.Content,
                            Status = "draft",
                            CreatedAt = DateTime.UtcNow,
                            Category = request.Category,
                            ImageUrl = request.ImageUrl
                        })).Models.FirstOrDefault();
                    if (ja != null)
                        created = new ContentItemDTO
                        {
                            Id = ja.Id,
                            Title = ja.Title,
                            Content = ja.Content,
                            Status = ja.Status,
                            Views = ja.Views,
                            CreatedAt = ja.CreatedAt,
                            PublishedAt = ja.PublishedAt,
                            Category = ja.Category,
                            ImageUrl = ja.ImageUrl

                        };
                    break;

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
                            ImageUrl = request.ImageUrl

                        })).Models.FirstOrDefault();
                    if (dh != null)
                        created = new ContentItemDTO
                        {
                            Id = dh.Id,
                            Title = dh.Title,
                            Content = dh.Content,
                            Status = dh.Status,
                            Views = dh.Views,
                            CreatedAt = dh.CreatedAt,
                            PublishedAt = dh.PublishedAt,
                            Designer = dh.Designer,
                            Year = dh.Year,
                            Inspiration = dh.Inspiration,
                            Collection = dh.Collection,
                            SecondImageUrl = dh.SecondImageUrl,
                            TechnicalFabric = dh.TechnicalFabric,
                            TechnicalTechniques = dh.TechnicalTechniques,
                            TechnicalProduction = dh.TechnicalProduction,
                            TechnicalAvailability = dh.TechnicalAvailability,
                            ImageUrl = dh.ImageUrl

                        };
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

    // =============================================
    // UPDATE
    // =============================================
    public async Task<Response<ContentItemDTO>> Update(
        ContentCategory type, Guid id, UpdateContentItemDTO request)
    {
        try
        {
            ContentItemDTO? updated = null;

            switch (type)
            {
                case ContentCategory.JournalArticles:
                    var ja = (await _client.From<JournalArticle>()
         .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
             id.ToString())
         .Set(x => x.Title, request.Title)
         .Set(x => x.Content!, request.Content)
         .Set(x => x.Category!, request.Category)  // ← add
         .Set(x => x.ImageUrl!, request.ImageUrl)  // ← add
         .Update()).Models.FirstOrDefault();
                    if (ja != null)
                        updated = new ContentItemDTO
                        {
                            Id = ja.Id,
                            Title = ja.Title,
                            Content = ja.Content,
                            Status = ja.Status,
                            Views = ja.Views,
                            CreatedAt = ja.CreatedAt,
                            PublishedAt = ja.PublishedAt,
                            Category = ja.Category,
                            ImageUrl = ja.ImageUrl
                        };
                    break;

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
                        .Update()).Models.FirstOrDefault();
                    if (dh != null)
                        updated = new ContentItemDTO
                        {
                            Id = dh.Id,
                            Title = dh.Title,
                            Content = dh.Content,
                            Status = dh.Status,
                            Views = dh.Views,
                            CreatedAt = dh.CreatedAt,
                            PublishedAt = dh.PublishedAt,
                            Designer = dh.Designer,
                            Year = dh.Year,
                            Inspiration = dh.Inspiration,
                            Collection = dh.Collection,
                            SecondImageUrl = dh.SecondImageUrl,
                            TechnicalFabric = dh.TechnicalFabric,
                            TechnicalTechniques = dh.TechnicalTechniques,
                            TechnicalProduction = dh.TechnicalProduction,
                            TechnicalAvailability = dh.TechnicalAvailability,
                            ImageUrl = dh.ImageUrl
                        };
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

    // =============================================
    // DELETE
    // =============================================
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

    // =============================================
    // PUBLISH
    // =============================================
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
                        .Update()).Models.FirstOrDefault();
                    if (ja != null)
                        published = new ContentItemDTO
                        {
                            Id = ja.Id,
                            Title = ja.Title,
                            Content = ja.Content,
                            Status = ja.Status,
                            Views = ja.Views,
                            CreatedAt = ja.CreatedAt,
                            PublishedAt = ja.PublishedAt,
                            Category = ja.Category,   // ← add
                            ImageUrl = ja.ImageUrl
                        };
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
                        published = new ContentItemDTO
                        {
                            Id = dh.Id,
                            Title = dh.Title,
                            Content = dh.Content,
                            Status = dh.Status,
                            Views = dh.Views,
                            CreatedAt = dh.CreatedAt,
                            PublishedAt = dh.PublishedAt
                        };
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

    // =============================================
    // UNPUBLISH
    // =============================================
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
                        .Update()).Models.FirstOrDefault();
                    if (ja != null)
                        unpublished = new ContentItemDTO
                        {
                            Id = ja.Id,
                            Title = ja.Title,
                            Content = ja.Content,
                            Status = ja.Status,
                            Views = ja.Views,
                            CreatedAt = ja.CreatedAt,
                            PublishedAt = ja.PublishedAt,
                            Category = ja.Category,   // ← add
                            ImageUrl = ja.ImageUrl
                        };
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
                        unpublished = new ContentItemDTO
                        {
                            Id = dh.Id,
                            Title = dh.Title,
                            Content = dh.Content,
                            Status = dh.Status,
                            Views = dh.Views,
                            CreatedAt = dh.CreatedAt,
                            PublishedAt = dh.PublishedAt
                        };
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
            //  stream directly — no full byte load
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

    // =============================================
    // GET PUBLISHED (paginated + filtered)
    // =============================================
    public async Task<Response<PaginatedResponse<ContentItemDTO>>> GetPublished(
        int page, int pageSize, string? category = null)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            // Count uses HEAD; run before page GET to avoid parallel PostgREST conflicts.
            var totalCount = await CountPublishedArticles(category);
            var items = await FetchPublishedPage(offset, pageSize, category);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedResult = new PaginatedResponse<ContentItemDTO>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = page < totalPages
            };

            return Response<PaginatedResponse<ContentItemDTO>>.SuccessResponse(
                pagedResult, "Articles fetched");
        }
        catch (Exception ex)
        {
            return Response<PaginatedResponse<ContentItemDTO>>.Fail("Error: " + ex.Message);
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
            .Select(x => new ContentItemDTO
            {
                Id = x.Id,
                Title = x.Title,
                Category = x.Category,
                ImageUrl = x.ImageUrl,
                Status = x.Status,
                Views = x.Views,
                CreatedAt = x.CreatedAt,
                PublishedAt = x.PublishedAt
            }).ToList();
    }
    // =============================================
    // GET PUBLISHED DESIGN HISTORY (for public Archive page)
    // =============================================
    public async Task<Response<List<ContentItemDTO>>> GetPublishedDesignHistory()
    {
        try
        {
            var result = await _client.From<DesignHistory>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals,
                    "published")
                .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var items = result.Models.Select(x => new ContentItemDTO
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                Status = x.Status,
                Views = x.Views,
                CreatedAt = x.CreatedAt,
                PublishedAt = x.PublishedAt,
                Designer = x.Designer,
                Year = x.Year,
                Inspiration = x.Inspiration,
                Collection = x.Collection,
                SecondImageUrl = x.SecondImageUrl,
                TechnicalFabric = x.TechnicalFabric,
                TechnicalTechniques = x.TechnicalTechniques,
                TechnicalProduction = x.TechnicalProduction,
                TechnicalAvailability = x.TechnicalAvailability,
                ImageUrl = x.ImageUrl
            }).ToList();

            return Response<List<ContentItemDTO>>.SuccessResponse(
                items, "Design history fetched");
        }
        catch (Exception ex)
        {
            return Response<List<ContentItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

}