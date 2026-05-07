using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("order_returns")]
public class OrderReturn : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("return_number")]
    public string ReturnNumber { get; set; } = string.Empty;

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("items_to_return")]
    public string ItemsToReturn { get; set; } = string.Empty;

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
