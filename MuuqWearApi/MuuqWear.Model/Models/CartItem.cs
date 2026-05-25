using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.API.Models;

[Table("cart_items")]
public class CartItem : BaseModel
{
    // primary key — database generates automatically
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    // links to Supabase auth user
    [Column("user_id")]
    public Guid UserId { get; set; }

    // links to product
    [Column("product_id")]
    public Guid ProductId { get; set; }

    // size selected by user e.g. "M", "XL"
    [Column("size")]
    public string Size { get; set; } = string.Empty;

    // how many of this item
    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    // when item was added to cart
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("color")]
    public string Color { get; set; } = string.Empty;
    [Column("is_affiliate_discount")]
    public bool IsAffiliateDiscount { get; set; } = false;
    [Column("product_price")]
    public decimal ProductPrice { get; set; } = 0;
}