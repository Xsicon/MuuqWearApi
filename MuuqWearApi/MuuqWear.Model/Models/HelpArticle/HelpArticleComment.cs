using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.HelpArticle;

[Table("help_article_comments")]
public class HelpArticleComment : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("article_id")]
    public Guid ArticleId { get; set; }

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
