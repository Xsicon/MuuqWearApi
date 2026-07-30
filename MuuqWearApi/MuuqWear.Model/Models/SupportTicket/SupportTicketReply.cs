using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.SupportTicket;

[Table("support_ticket_replies")]
public class SupportTicketReply : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("sender_type")]
    public string SenderType { get; set; } = "agent";

    [Column("sender_id")]
    public Guid? SenderId { get; set; }

    [Column("sender_name")]
    public string? SenderName { get; set; }

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
