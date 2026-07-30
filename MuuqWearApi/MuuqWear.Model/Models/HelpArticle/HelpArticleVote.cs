using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.HelpArticle;

[Table("help_article_votes")]
public class HelpArticleVote : BaseModel
{
    [PrimaryKey("article_id", false)]
    public Guid ArticleId { get; set; }

    [PrimaryKey("voter_key", false)]
    public string VoterKey { get; set; } = string.Empty;

    [Column("vote")]
    public string Vote { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
