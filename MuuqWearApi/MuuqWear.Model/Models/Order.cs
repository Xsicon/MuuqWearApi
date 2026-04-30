using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("orders")]
public class Order : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("shipping")]
    public decimal Shipping { get; set; }

    [Column("tax")]
    public decimal Tax { get; set; }

    [Column("total")]
    public decimal Total { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("postal_code")]
    public string? PostalCode { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
