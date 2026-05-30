using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Chat;

[Table("chat_sessions")]
public class ChatSession : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("guest_name")]
    public string? GuestName { get; set; }

    [Column("guest_email")]
    public string? GuestEmail { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }
}