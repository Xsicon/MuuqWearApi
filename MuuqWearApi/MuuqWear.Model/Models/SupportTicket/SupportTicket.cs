using MuuqWear.Model.DTO.HelpCenterDTO;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.SupportTicket;

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

    [Column("assigned_to")]
    public Guid? AssignedTo { get; set; }

    [Column("assigned_to_name")]
    public string? AssignedToName { get; set; }

    [Column("team")]
    public string? Team { get; set; }

    [Column("first_response_at")]
    public DateTime? FirstResponseAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
