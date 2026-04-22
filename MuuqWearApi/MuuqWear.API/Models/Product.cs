using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.API.Models;
[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("badge")]
    public string? Badge { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("stock")]
    public int Stock { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    [Column("is_new_arrival")]
    public bool IsNewArrival { get; set; }

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("is_best_seller")]
    public bool IsBestSeller { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("sizes")]
    public string? Sizes { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }
}
