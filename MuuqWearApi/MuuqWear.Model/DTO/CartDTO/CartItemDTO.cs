using System.Text.Json.Serialization;

namespace MuuqWear.Model.DTO.CartDTO;

public class CartItemDTO
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

    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public decimal ItemTotal => ProductPrice * Quantity;
}


public class AddCartItemDTO
{
    public Guid ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

// used when updating quantity
public class UpdateCartItemDTO
{
    public Guid CartItemId { get; set; }
    public int Quantity { get; set; }
}


