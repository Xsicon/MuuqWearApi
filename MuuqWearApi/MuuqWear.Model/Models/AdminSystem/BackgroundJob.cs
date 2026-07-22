using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AdminSystem;

[Table("background_jobs")]
public class BackgroundJob : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("job_key")]
    public string JobKey { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("schedule")]
    public string Schedule { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "idle";

    [Column("last_run_at")]
    public DateTime? LastRunAt { get; set; }

    [Column("next_run_at")]
    public DateTime? NextRunAt { get; set; }

    [Column("last_run_message")]
    public string? LastRunMessage { get; set; }
}
