using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.HelpArticle;

[Table("help_article_steps")]
public class HelpArticleStep : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("article_id")]
    public Guid ArticleId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("detail")]
    public string Detail { get; set; } = string.Empty;

    [Column("image_url")]
    public string? ImageUrl { get; set; }
}
