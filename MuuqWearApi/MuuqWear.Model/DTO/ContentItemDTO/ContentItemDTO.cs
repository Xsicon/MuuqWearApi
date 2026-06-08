namespace MuuqWear.Model.DTO.ContentItemDTO;
public class ContentItemDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = "draft";
    public int Views { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished => Status == "published"; // ← computed
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }
    public string? Designer { get; set; }
    public string? Year { get; set; }
    public string? Inspiration { get; set; }
    public string? Collection { get; set; }
    public string? SecondImageUrl { get; set; }
    public string? TechnicalFabric { get; set; }
    public string? TechnicalTechniques { get; set; }
    public string? TechnicalProduction { get; set; }
    public string? TechnicalAvailability { get; set; }
}

public class CreateContentItemDTO
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
    public string? Designer { get; set; }
    public string? Year { get; set; }
    public string? Inspiration { get; set; }
    public string? Collection { get; set; }
    public string? SecondImageUrl { get; set; }
    public string? TechnicalFabric { get; set; }
    public string? TechnicalTechniques { get; set; }
    public string? TechnicalProduction { get; set; }
    public string? TechnicalAvailability { get; set; }

}

public class UpdateContentItemDTO
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
    public string? Designer { get; set; }
    public string? Year { get; set; }
    public string? Inspiration { get; set; }
    public string? Collection { get; set; }
    public string? SecondImageUrl { get; set; }
    public string? TechnicalFabric { get; set; }
    public string? TechnicalTechniques { get; set; }
    public string? TechnicalProduction { get; set; }
    public string? TechnicalAvailability { get; set; }
}


public enum ContentCategory
{
    JournalArticles,
    Events,
    DesignHistory
}