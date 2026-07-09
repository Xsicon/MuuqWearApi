using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.CustomerNote;

[Table("customer_notes")]
public class CustomerNote : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("author_user_id")]
    public Guid AuthorUserId { get; set; }

    [Column("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [Column("author_role")]
    public string? AuthorRole { get; set; }

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
