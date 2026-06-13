using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Product;
[Table("product_images")]
public class ProductImage : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("product_id")]
    public Guid ProductId { get; set; }
    [Column("image_url")]
    public string? ImageUrl { get; set; }
    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
