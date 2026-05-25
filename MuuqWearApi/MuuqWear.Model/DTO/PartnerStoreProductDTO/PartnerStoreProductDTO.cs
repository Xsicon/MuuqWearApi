namespace MuuqWear.Model.DTO.PartnerStoreProductDTO;

public class PartnerStoreProductDTO
{
    /// <summary>
    /// Product unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Product category (Outerwear, Accessories, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Primary product image URL
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Original retail price
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Price after affiliate discount (25% off)
    /// </summary>
    public decimal DiscountedPrice { get; set; }

    /// <summary>
    /// Dollar amount saved with discount
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Discount percentage (always 25 for now)
    /// </summary>
    public int DiscountPercentage { get; set; } = 25;

    /// <summary>
    /// Whether product has any size in stock
    /// </summary>
    public bool InStock { get; set; }

    /// <summary>
    /// Available sizes with stock quantities
    /// </summary>
    public List<ProductSizeStockDTO> SizeStock { get; set; } = new();

    /// <summary>
    /// Total quantity across all sizes
    /// </summary>
    public int TotalStock { get; set; }
}

/// <summary>
/// Represents stock for a single size
/// </summary>
public class ProductSizeStockDTO
{
    /// <summary>
    /// Size (S, M, L, XL, etc.)
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Quantity available for this size
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Whether this size is in stock
    /// </summary>
    public bool InStock => Quantity > 0;
}