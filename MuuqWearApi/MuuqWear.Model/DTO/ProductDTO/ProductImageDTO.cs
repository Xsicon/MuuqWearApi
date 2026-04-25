namespace MuuqWear.API.DTO.ProductDTO;
public class ProductImageDTO
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class AddProductImageDTO
{
    public Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class DeleteProductImageDTO
{
    public Guid Id { get; set; }
}
