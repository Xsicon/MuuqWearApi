using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.JournalArticle;

[Table("journal_articles")]
public class JournalArticle : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("views")]
    public int Views { get; set; } = 0;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("author")]
    public string? Author { get; set; }

    [Column("excerpt")]
    public string? Excerpt { get; set; }

    [Column("slug")]
    public string? Slug { get; set; }

    [Column("seo_title")]
    public string? SeoTitle { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("read_time_minutes")]
    public int? ReadTimeMinutes { get; set; }
}
