using System.Text.Json;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.MuuqsimoDTO;
using MuuqWear.Model.Models.Event;
using MuuqWear.Model.Models.Product;
using Supabase;

namespace MuuqWear.API.Service;

public class MuuqsimoService : IMuuqsimoService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<MuuqsimoTicketTierConfigDTO> DefaultTierConfigs =
    [
        new()
        {
            ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TotalCapacity = 250,
            Perks = ["Standard admission", "Open bar & hors d'oeuvres", "Runway show & awards ceremony"]
        },
        new()
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TotalCapacity = 150,
            IsHighlighted = true,
            Badge = "Most Popular",
            Perks =
            [
                "Priority front-section seating",
                "Welcome gift bag",
                "After-party access",
                "All Veil Access perks"
            ]
        },
        new()
        {
            ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            TotalCapacity = 75,
            Perks =
            [
                "Front row seating",
                "VIP lounge access",
                "Meet-and-greet with designers",
                "After-party VIP section",
                "Luxury hotel room upgrade",
                "All Sapphire Circle perks"
            ]
        }
    ];

    private readonly Client _client;

    public MuuqsimoService(SupabaseAdminClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    public async Task<Response<MuuqsimoPageDTO>> GetPublishedPageAsync(string? slug = null)
    {
        try
        {
            slug ??= "muuqsimo-2025";

            var eventsResult = await _client.From<Event>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var match = FindEventBySlug(eventsResult.Models, slug)
                        ?? eventsResult.Models.FirstOrDefault();

            if (match == null)
                return Response<MuuqsimoPageDTO>.Fail("No published Muuqsimo event found.");

            if (string.IsNullOrWhiteSpace(match.Content))
                return Response<MuuqsimoPageDTO>.Fail("Event content is empty.");

            var content = JsonSerializer.Deserialize<MuuqsimoPageContentDTO>(
                match.Content, JsonOptions);

            if (content == null)
                return Response<MuuqsimoPageDTO>.Fail("Failed to parse event content.");

            var tierConfigs = content.Tickets?.TierConfigs is { Count: > 0 } configured
                ? configured
                : DefaultTierConfigs;

            var ticketTiers = await BuildTicketTiersAsync(tierConfigs);

            return Response<MuuqsimoPageDTO>.SuccessResponse(
                new MuuqsimoPageDTO
                {
                    EventId = match.Id,
                    Title = match.Title,
                    Content = content,
                    TicketTiers = ticketTiers
                },
                "Muuqsimo page fetched");
        }
        catch (Exception ex)
        {
            return Response<MuuqsimoPageDTO>.Fail("Error: " + ex.Message);
        }
    }

    private static Event? FindEventBySlug(IEnumerable<Event> events, string slug)
    {
        foreach (var ev in events)
        {
            if (string.IsNullOrWhiteSpace(ev.Content))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(ev.Content);
                if (doc.RootElement.TryGetProperty("slug", out var slugProp) &&
                    string.Equals(slugProp.GetString(), slug, StringComparison.OrdinalIgnoreCase))
                {
                    return ev;
                }
            }
            catch (JsonException)
            {
                // ignore malformed rows
            }
        }

        return null;
    }

    private async Task<List<MuuqsimoTicketTierDTO>> BuildTicketTiersAsync(
        List<MuuqsimoTicketTierConfigDTO> tierConfigs)
    {
        var productIds = tierConfigs.Select(t => t.ProductId).ToHashSet();
        if (productIds.Count == 0)
            return [];

        var productsResult = await _client.From<Product>()
            .Filter("is_ticket", Supabase.Postgrest.Constants.Operator.Equals, "true")
            .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
            .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
            .Get();

        var products = productsResult.Models
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var stockResult = await _client.From<ProductSizeStock>().Get();
        var stockByProduct = stockResult.Models
            .Where(s => productIds.Contains(s.ProductId))
            .GroupBy(s => s.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Quantity));

        var tiers = new List<MuuqsimoTicketTierDTO>();

        foreach (var config in tierConfigs)
        {
            if (!products.TryGetValue(config.ProductId, out var product))
                continue;

            stockByProduct.TryGetValue(config.ProductId, out var stock);

            tiers.Add(new MuuqsimoTicketTierDTO
            {
                ProductId = config.ProductId,
                Name = product.Name ?? "Ticket",
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Stock = stock,
                Availability = config.TotalCapacity > 0
                    ? $"{stock} of {config.TotalCapacity} remaining"
                    : $"{stock} remaining",
                Perks = config.Perks,
                IsHighlighted = config.IsHighlighted,
                Badge = config.Badge ?? product.Badge
            });
        }

        return tiers.OrderBy(t => t.Price).ToList();
    }

    public async Task<Response<List<MuuqsimoEventSummaryDTO>>> GetPublishedEventsAsync()
    {
        try
        {
            var eventsResult = await _client.From<Event>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "published")
                .Order("published_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var summaries = new List<MuuqsimoEventSummaryDTO>();

            foreach (var ev in eventsResult.Models)
            {
                if (string.IsNullOrWhiteSpace(ev.Content))
                    continue;

                try
                {
                    var content = JsonSerializer.Deserialize<MuuqsimoPageContentDTO>(
                        ev.Content, JsonOptions);

                    if (content == null || string.IsNullOrWhiteSpace(content.Slug))
                        continue;

                    summaries.Add(new MuuqsimoEventSummaryDTO
                    {
                        EventId = ev.Id,
                        Title = ev.Title,
                        Slug = content.Slug,
                        Tagline = content.Tagline,
                        StartDate = content.StartDate
                    });
                }
                catch (JsonException)
                {
                    // skip malformed rows
                }
            }

            return Response<List<MuuqsimoEventSummaryDTO>>.SuccessResponse(
                summaries,
                "Published Muuqsimo events fetched");
        }
        catch (Exception ex)
        {
            return Response<List<MuuqsimoEventSummaryDTO>>.Fail("Error: " + ex.Message);
        }
    }
}
