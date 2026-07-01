using Microsoft.Extensions.Caching.Memory;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.VoteDTO;
using MuuqWear.Model.Models.PreOrderInterest;
using MuuqWear.Model.Models.UserVote;
using System.Text.Json;

namespace MuuqWear.Application.Service;

public class VoteService : IVoteService
{
    private readonly Supabase.Client _client;
    private readonly IMemoryCache _cache;

    public VoteService(SupabaseClientFactory factory, IMemoryCache cache)
    {
        _client = factory.CreateClient();
        _cache = cache;
    }

    // =============================================
    // GET ACTIVE ITEMS
    // =============================================
    public async Task<Response<List<VoteItemDTO>>> GetActiveItems(Guid userId)
    {
        try
        {
            var itemsTask = _client
                .From<VoteItem>()
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    VoteItemStatus.Active)
                .Order("vote_count",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            if (userId == Guid.Empty)
            {
                var publicResult = await itemsTask;
                var publicItems = publicResult.Models.Select(v => new VoteItemDTO
                {
                    Id = v.Id,
                    StyleName = v.StyleName,
                    Subtitle = v.Subtitle,
                    Description = v.Description,
                    ImageUrl = v.ImageUrl,
                    Tag = v.Tag,
                    VoteCount = v.VoteCount,
                    ColorOptions = v.ColorOptions ?? new List<string>(),
                    Status = v.Status,
                    Season = v.Season,
                    CreatedAt = v.CreatedAt,
                    HasVoted = false,
                    HasPreOrdered = false
                }).ToList();

                return Response<List<VoteItemDTO>>
                    .SuccessResponse(publicItems, "Active items fetched");
            }

            var userVotesTask = _client
                .From<UserVote>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            var preOrdersTask = _client
                .From<PreOrderInterest>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            await Task.WhenAll(itemsTask, userVotesTask, preOrdersTask);

            var result = await itemsTask;
            var userVotes = await userVotesTask;
            var preOrders = await preOrdersTask;

            var userVoteByItem = userVotes.Models
                .ToDictionary(v => v.VoteItemId, v => v.PreferredColor);

            var preOrderedIds = preOrders.Models
                .Select(p => p.VoteItemId)
                .ToHashSet();

            var items = result.Models.Select(v => new VoteItemDTO
            {
                Id = v.Id,
                StyleName = v.StyleName,
                Subtitle = v.Subtitle,
                Description = v.Description,
                ImageUrl = v.ImageUrl,
                Tag = v.Tag,
                VoteCount = v.VoteCount,
                ColorOptions = v.ColorOptions ?? new List<string>(),
                Status = v.Status,
                Season = v.Season,
                CreatedAt = v.CreatedAt,
                HasVoted = userVoteByItem.ContainsKey(v.Id),
                HasPreOrdered = preOrderedIds.Contains(v.Id),
                VotedColor = userVoteByItem.GetValueOrDefault(v.Id)
            }).ToList();

            return Response<List<VoteItemDTO>>
                .SuccessResponse(items, "Active items fetched");
        }
        catch (Exception ex)
        {
            return Response<List<VoteItemDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET FINISHED ITEMS
    // =============================================
    public async Task<Response<List<VoteItemDTO>>> GetFinishedItems()
    {
        try
        {
            //  fetch both finished and production items
            var result = await _client
                .From<VoteItem>()
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.In,
                    new List<string>
                    {
                        VoteItemStatus.Finished,
                        VoteItemStatus.Production
                    })
                .Order("vote_count",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var items = result.Models.Select(v => new VoteItemDTO
            {
                Id = v.Id,
                StyleName = v.StyleName,
                Subtitle = v.Subtitle,
                Description = v.Description,
                ImageUrl = v.ImageUrl,
                Tag = v.Tag,
                VoteCount = v.VoteCount,
                ColorOptions = v.ColorOptions ?? new List<string>(), // ← direct 
                Status = v.Status,
                Season = v.Season,
                CreatedAt = v.CreatedAt
            }).ToList();

            return Response<List<VoteItemDTO>>
                .SuccessResponse(items, "Finished items fetched");
        }
        catch (Exception ex)
        {
            return Response<List<VoteItemDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // GET STATS
    // =============================================
    public async Task<Response<VoteStatsDTO>> GetStats()
    {
        if (_cache.TryGetValue(ApiCacheKeys.VoteStats, out VoteStatsDTO? cached)
            && cached != null)
        {
            return Response<VoteStatsDTO>.SuccessResponse(cached, "Stats fetched");
        }

        try
        {
            var result = await _client.Rpc("get_vote_stats",
                new Dictionary<string, object>());

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            //  RPC returns array → take first element
            var stats = JsonSerializer
                .Deserialize<List<VoteStatsDTO>>(
                    result.Content ?? "[]", options)
                ?.FirstOrDefault()
                ?? new VoteStatsDTO();

            await EnrichStatsFromVotesAsync(stats);

            _cache.Set(ApiCacheKeys.VoteStats, stats, ApiCacheKeys.ReadTtl);

            return Response<VoteStatsDTO>
                .SuccessResponse(stats, "Stats fetched");
        }
        catch (Exception ex)
        {
            return Response<VoteStatsDTO>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CAST VOTE
    // =============================================
    public async Task<Response<VoteItemDTO>> CastVote(
     Guid userId, CastVoteDTO request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PreferredColor))
            {
                return Response<VoteItemDTO>.Fail(
                    "Please select a color before voting.");
            }

            //  single atomic RPC call
            // insert vote + increment count in one transaction
            var rpcParams = new Dictionary<string, object>
            {
                { "p_vote_item_id", request.VoteItemId.ToString() },
                { "p_user_id", userId.ToString() },
                { "p_preferred_color", request.PreferredColor!.Trim() }
            };

            var result = await _client.Rpc("cast_vote", rpcParams);

            //  RPC returns new vote count as integer
            if (!int.TryParse(result.Content?.Trim('"'), out var newCount))
                return Response<VoteItemDTO>.Fail(
                    "Failed to cast vote. Please try again.");

            _cache.Remove(ApiCacheKeys.VoteStats);

            return Response<VoteItemDTO>.SuccessResponse(
                new VoteItemDTO
                {
                    Id = request.VoteItemId,
                    VoteCount = newCount,
                    HasVoted = true,
                    VotedColor = request.PreferredColor!.Trim()
                }, "Vote cast successfully");
        }
        catch (Exception ex)
        {
            //  UNIQUE constraint violation = already voted
            // Supabase returns 23505 for unique violation
            if (ex.Message.Contains("23505") ||
                ex.Message.Contains("duplicate") ||
                ex.Message.Contains("unique") ||
                ex.Message.Contains("already exists"))
                return Response<VoteItemDTO>.Fail(
                    "You have already voted for this item");

            return Response<VoteItemDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // REGISTER PRE-ORDER
    // =============================================
    public async Task<Response<bool>> RegisterPreOrder(
        Guid userId, PreOrderDTO request)
    {
        try
        {
            // Step 1 — check item exists
            var item = await _client
                .From<VoteItem>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.VoteItemId.ToString())
                .Single();

            if (item == null)
                return Response<bool>.Fail("Item not found");

            // Step 2 — check not already pre-ordered
            var existing = await _client
                .From<PreOrderInterest>()
                .Filter("vote_item_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.VoteItemId.ToString())
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            if (existing.Models.Any())
                return Response<bool>.Fail(
                    "You have already registered interest for this item");

            // Step 3 — insert pre-order interest
            await _client
                .From<PreOrderInterest>()
                .Insert(new PreOrderInterest
                {
                    Id = Guid.NewGuid(),
                    VoteItemId = request.VoteItemId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

            return Response<bool>.SuccessResponse(
                true, "Pre-order interest registered successfully");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // PRIVATE HELPERS
    // =============================================
    private async Task EnrichStatsFromVotesAsync(VoteStatsDTO stats)
    {
        var weekStart = GetUtcWeekStart();

        var result = await _client
            .From<UserVote>()
            .Get();

        var weekVotes = result.Models
            .Where(v => v.CreatedAt.HasValue && v.CreatedAt.Value >= weekStart)
            .ToList();

        if (stats.TotalVotesThisWeek <= 0)
            stats.TotalVotesThisWeek = weekVotes.Count;

        if (!string.IsNullOrWhiteSpace(stats.MostWantedColor))
        {
            stats.MostWantedColor = NormalizeColorHex(stats.MostWantedColor);
            return;
        }

        stats.MostWantedColor = weekVotes
            .Where(v => !string.IsNullOrWhiteSpace(v.PreferredColor))
            .Select(v => NormalizeColorHex(v.PreferredColor!))
            .Where(c => !string.IsNullOrEmpty(c))
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    private static DateTime GetUtcWeekStart()
    {
        var now = DateTime.UtcNow;
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        return now.Date.AddDays(-daysSinceMonday);
    }

    private static string NormalizeColorHex(string color)
    {
        var trimmed = color.Trim();
        if (trimmed.Length == 0)
            return trimmed;

        if (!trimmed.StartsWith('#') &&
            trimmed.Length == 6 &&
            trimmed.All(Uri.IsHexDigit))
        {
            return "#" + trimmed;
        }

        return trimmed;
    }

    //  single responsibility — parses JSON color array
    // e.g. '["#1E2A47","#4A5C7A"]' → List<string>
    private List<string> ParseColors(string? colorJson)
    {
        if (string.IsNullOrEmpty(colorJson))
            return new List<string>();

        try
        {
            return JsonSerializer
                .Deserialize<List<string>>(colorJson)
                ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
