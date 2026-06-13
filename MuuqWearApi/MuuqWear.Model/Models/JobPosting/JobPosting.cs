using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.JobPosting;

[Table("job_postings")]
public class JobPosting : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("department")]
    public string Department { get; set; } = string.Empty;

    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "open";

    [Column("application_count")]
    public int ApplicationCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("opened_at")]
    public DateTime? OpenedAt { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }
}
