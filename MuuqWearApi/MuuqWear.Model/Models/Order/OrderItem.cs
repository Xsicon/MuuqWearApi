using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Order;
[Table("order_items")]
public class OrderItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    // snapshot fields 
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("product_image_url")]
    public string? ProductImageUrl { get; set; }

    [Column("size")]
    public string Size { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("item_total")]
    public decimal ItemTotal { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}

