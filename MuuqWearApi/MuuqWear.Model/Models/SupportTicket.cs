using MuuqWear.Model.DTO.HelpCenterDTO;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("support_tickets")]
public class SupportTicket : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("ticket_number")]
    public string TicketNumber { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("priority")]
    public string Priority { get; set; } = TicketPriority.Normal;

    [Column("status")]
    public string Status { get; set; } = TicketStatus.Open;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
