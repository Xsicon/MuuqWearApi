namespace MuuqWear.Model.DTO.HelpCenterDTO;

public class HelpArticleDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = HelpArticleStatus.Draft;
    public string? HeroImageUrl { get; set; }
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<HelpArticleStepDTO> Steps { get; set; } = [];

    /// <summary>Admin/support only — never populated for public article reads.</summary>
    public List<HelpArticleCommentDTO> Comments { get; set; } = [];

    /// <summary>Admin engagement counts (like/dislike).</summary>
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }

    /// <summary>Current admin user's vote: like | dislike | null.</summary>
    public string? MyVote { get; set; }
}

public class HelpArticleStepDTO
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class HelpArticleCommentDTO
{
    public Guid Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddHelpArticleCommentDTO
{
    public string Body { get; set; } = string.Empty;
}

public class HelpArticleVoteRequestDTO
{
    public string Vote { get; set; } = string.Empty;
}

public class HelpArticleEngagementDTO
{
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public string? MyVote { get; set; }
    public List<HelpArticleCommentDTO> Comments { get; set; } = [];
}

public class SaveHelpArticleDTO
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = HelpArticleStatus.Draft;
    public string? HeroImageUrl { get; set; }
    public List<HelpArticleStepDTO> Steps { get; set; } = [];
}

public class UpdateHelpArticleStatusDTO
{
    public string Status { get; set; } = string.Empty;
}

public static class HelpArticleStatus
{
    public const string Draft = "draft";
    public const string Published = "published";

    public static readonly string[] All = [Draft, Published];

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Draft;

        return status.Trim().Equals(Published, StringComparison.OrdinalIgnoreCase)
            ? Published
            : Draft;
    }
}

public static class HelpArticleVoteValue
{
    public const string Like = "like";
    public const string Dislike = "dislike";

    public static readonly string[] All = [Like, Dislike];

    public static string? Normalize(string? vote)
    {
        if (string.IsNullOrWhiteSpace(vote))
            return null;

        var trimmed = vote.Trim().ToLowerInvariant();
        return All.Contains(trimmed) ? trimmed : null;
    }
}

public static class HelpArticleCategory
{
    public static readonly string[] All =
    [
        "Orders", "Shipping", "Returns", "Payments", "Account", "Product Info"
    ];
}
