using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.JobApplication;

[Table("job_applications")]

public class JobApplication : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    [Column("job_id")]
    public Guid JobId { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    [Column("portfolio_url")]
    public string? PortFolioUrl { get; set; }
    [Column("resume_url")]
    public string ResumeUrl { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "new";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }


}
