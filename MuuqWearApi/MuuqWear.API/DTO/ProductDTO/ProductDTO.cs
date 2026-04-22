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
    public string? Sizes { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<ProductImageDTO> Images { get; set; } = new();

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
    public string? Sizes { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
}
