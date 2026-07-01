using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.WishlistItem;

[Table("wishlist_items")]
public class WishlistItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
