using System.Text.Json.Serialization;

namespace MuuqWear.Model.DTO.WishlistDTO;

public class WishlistItemDTO
{
    public Guid Id { get; set; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_image_url")]
    public string? ProductImageUrl { get; set; }

    [JsonPropertyName("product_price")]
    public decimal ProductPrice { get; set; }

    [JsonPropertyName("added_at")]
    public DateTime? AddedAt { get; set; }
}

public class AddWishlistItemDTO
{
    public Guid ProductId { get; set; }
}

public class MergeWishlistRequestDTO
{
    public List<Guid> ProductIds { get; set; } = new();
}
