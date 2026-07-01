namespace MuuqWear.Model.DTO.VoteDTO;
// ─── VOTE ITEM DTO ───────────────────────────────────────────
// used for both active and finished items
public class VoteItemDTO
{
    public Guid Id { get; set; }
    public string? StyleName { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Tag { get; set; }
    public int VoteCount { get; set; }
    public List<string> ColorOptions { get; set; } = new();
    public string? Status { get; set; }
    public string? Season { get; set; }
    public DateTime? CreatedAt { get; set; }

    //  computed — has current user voted for this item
    // set server side per request 
    public bool HasVoted { get; set; }

    //  computed — has current user pre-ordered this item
    public bool HasPreOrdered { get; set; }

    // color the current user picked when they voted
    public string? VotedColor { get; set; }
}

// ─── VOTE STATS DTO ──────────────────────────────────────────
// used for stats bar
public class VoteStatsDTO
{
    public long TotalVotesThisWeek { get; set; }
    public string? MostWantedColor { get; set; }
    public string? NextDeadline { get; set; }
}

// ─── CAST VOTE DTO ───────────────────────────────────────────
// used when user votes
public class CastVoteDTO
{
    public Guid VoteItemId { get; set; }
    public string? PreferredColor { get; set; }
}

// ─── PRE ORDER DTO ───────────────────────────────────────────
// used when user pre-orders
public class PreOrderDTO
{
    public Guid VoteItemId { get; set; }
}

// ─── VOTE ITEM STATUS ────────────────────────────────────────
//  constants — no magic strings
public static class VoteItemStatus
{
    public const string Active = "active";
    public const string Finished = "finished";
    public const string Production = "production";
}