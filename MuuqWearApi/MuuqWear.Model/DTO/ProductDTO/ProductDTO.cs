namespace MuuqWear.API.DTO.ProductDTO;
public class ProductDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public string? Badge { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsNewArrival { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public string? Description { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<ProductImageDTO> Images { get; set; } = new();
    public string? Sku { get; set; }                           // ← add
    public List<SizeStockDTO> SizeStock { get; set; } = new();

}

public class SizeStockDTO
{
    public Guid Id { get; set; }        // ← needed for update
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
public class AddProductDTO
{
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public string? Badge { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public bool IsNewArrival { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public string? Description { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
    public List<string> Sizes { get; set; } = new();
}

public class UpdateSizeStockDTO
{
    public int Quantity { get; set; }
}

public class UpdateStockDTO
{
    public int Stock { get; set; }
}

public class AddSizeStockDTO
{
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
