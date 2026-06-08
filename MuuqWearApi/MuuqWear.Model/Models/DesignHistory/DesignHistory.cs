using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.DesignHistory;

[Table("design_history")]
public class DesignHistory : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("views")]
    public int Views { get; set; } = 0;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }
    [Column("designer")]
    public string? Designer { get; set; }

    [Column("year")]
    public string? Year { get; set; }

    [Column("inspiration")]
    public string? Inspiration { get; set; }

    [Column("collection")]
    public string? Collection { get; set; }

    [Column("second_image_url")]
    public string? SecondImageUrl { get; set; }

    [Column("technical_fabric")]
    public string? TechnicalFabric { get; set; }

    [Column("technical_techniques")]
    public string? TechnicalTechniques { get; set; }

    [Column("technical_production")]
    public string? TechnicalProduction { get; set; }

    [Column("technical_availability")]
    public string? TechnicalAvailability { get; set; }
    [Column("image_url")]
    public string? ImageUrl { get; set; }
}
