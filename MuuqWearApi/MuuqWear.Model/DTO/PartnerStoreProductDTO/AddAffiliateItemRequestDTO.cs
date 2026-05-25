namespace MuuqWear.Model.DTO.PartnerStoreProductDTO;

/// <summary>
/// Request to add affiliate-discounted item to cart
/// </summary>
public class AddAffiliateItemRequestDTO
{
    /// <summary>
    /// Product ID to add
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity to add (default 1)
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Product size (if applicable)
    /// </summary>
    public string? Size { get; set; }

    /// <summary>
    /// Product color (if applicable)
    /// </summary>
    public string? Color { get; set; }
}
