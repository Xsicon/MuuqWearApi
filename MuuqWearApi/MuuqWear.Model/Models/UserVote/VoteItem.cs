using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.UserVote;

[Table("vote_items")]
public class VoteItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("style_name")]
    public string? StyleName { get; set; }

    [Column("subtitle")]
    public string? Subtitle { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("tag")]
    public string? Tag { get; set; }

    [Column("vote_count")]
    public int VoteCount { get; set; }

    [Column("color_options")]
    public List<string> ColorOptions { get; set; } = new();

    [Column("status")]
    public string? Status { get; set; }

    [Column("season")]
    public string? Season { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
