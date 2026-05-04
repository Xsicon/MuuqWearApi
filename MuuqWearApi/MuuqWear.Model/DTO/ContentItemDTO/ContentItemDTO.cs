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
}

public class CreateContentItemDTO
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
}

public class UpdateContentItemDTO
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }  // ← add
    public string? ImageUrl { get; set; }  // ← add
}


public enum ContentCategory
{
    JournalArticles,
    Events,
    DesignHistory
}