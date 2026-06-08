namespace MuuqWear.API.DTO.ProductDTO;
public class ProductFilterDTO
{
    // pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // search
    public string? Search { get; set; }

    // filters
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sizes { get; set; }
    // sorting
    public string? SortBy { get; set; } = "featured";
    public bool IncludeTickets { get; set; } = false;
}