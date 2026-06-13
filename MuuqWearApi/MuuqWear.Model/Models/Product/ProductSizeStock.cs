using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Product;

[Table("product_size_stock")]
public class ProductSizeStock : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("size")]
    public string Size { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
