using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.UserVote;

[Table("user_votes")]
public class UserVote : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("vote_item_id")]
    public Guid VoteItemId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
