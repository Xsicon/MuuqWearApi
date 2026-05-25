using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("affiliate_personal_purchases")]
public class AffiliatePersonalPurchase : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("original_price")]
    public decimal OriginalPrice { get; set; }

    [Column("discounted_price")]
    public decimal DiscountedPrice { get; set; }

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Column("discount_percentage")]
    public int DiscountPercentage { get; set; }

    [Column("status")]
    public string Status { get; set; } = "completed";

    [Column("purchased_at")]
    public DateTime PurchasedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
