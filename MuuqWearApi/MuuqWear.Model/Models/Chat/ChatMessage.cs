using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Chat;

[Table("chat_messages")]
public class ChatMessage : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("sender_type")]
    public string SenderType { get; set; } = string.Empty;

    [Column("sender_id")]
    public Guid? SenderId { get; set; }

    [Column("sender_name")]
    public string? SenderName { get; set; }

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;
}
