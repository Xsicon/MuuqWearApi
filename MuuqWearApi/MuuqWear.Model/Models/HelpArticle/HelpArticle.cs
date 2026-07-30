using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.HelpArticle;

[Table("help_articles")]
public class HelpArticle : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("hero_image_url")]
    public string? HeroImageUrl { get; set; }

    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("helpful_count")]
    public int HelpfulCount { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }
}
