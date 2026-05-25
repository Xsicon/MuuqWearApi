using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.VoteDTO;
using MuuqWear.Model.Models;
using System.Text.Json;

namespace MuuqWear.Application.Service;

public class VoteService : IVoteService
{
    private readonly Supabase.Client _client;

    public VoteService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // GET ACTIVE ITEMS
    // =============================================
    public async Task<Response<List<VoteItemDTO>>> GetActiveItems(Guid userId)
    {
        try
        {
            // Step 1 — fetch active vote items
            var result = await _client
                .From<VoteItem>()
                .Filter("status",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    VoteItemStatus.Active)
                .Order("vote_count",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            // Step 2 — fetch user votes in one query
            var userVotes = await _client
                .From<UserVote>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            var votedIds = userVotes.Models
                .Select(v => v.VoteItemId)
                .ToHashSet();

            // Step 3 — fetch user pre-orders in one query
            var preOrders = await _client
                .From<PreOrderInterest>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Get();

            var preOrderedIds = preOrders.Models
                .Select(p => p.VoteItemId)
                .ToHashSet();

            // Step 4 — map to DTOs
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
                CreatedAt = v.CreatedAt,
                HasVoted = votedIds.Contains(v.Id),
                HasPreOrdered = preOrderedIds.Contains(v.Id)
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
            //  single atomic RPC call
            // insert vote + increment count in one transaction
            var result = await _client.Rpc(
                "cast_vote",
                new Dictionary<string, object>
                {
                { "p_vote_item_id", request.VoteItemId.ToString() },
                { "p_user_id",      userId.ToString() }
                });

            //  RPC returns new vote count as integer
            if (!int.TryParse(result.Content?.Trim('"'), out var newCount))
                return Response<VoteItemDTO>.Fail(
                    "Failed to cast vote. Please try again.");

            return Response<VoteItemDTO>.SuccessResponse(
                new VoteItemDTO
                {
                    Id = request.VoteItemId,
                    VoteCount = newCount,
                    HasVoted = true
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
    // PRIVATE HELPER
    // =============================================
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
