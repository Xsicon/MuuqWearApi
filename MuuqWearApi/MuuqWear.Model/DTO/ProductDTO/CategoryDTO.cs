namespace MuuqWear.API.DTO.ProductDTO;
public class CategoryDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}
public class AddCategoryDTO
{
    public string? Name { get; set; }
}
