using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

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
    public string? Category { get; set; }  // ← add

    [Column("image_url")]
    public string? ImageUrl { get; set; }
}
