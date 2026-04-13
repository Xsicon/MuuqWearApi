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
}
