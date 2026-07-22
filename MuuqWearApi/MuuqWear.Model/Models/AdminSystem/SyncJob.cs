using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AdminSystem;

[Table("sync_jobs")]
public class SyncJob : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("job_key")]
    public string JobKey { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "queued";

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("records_affected")]
    public int? RecordsAffected { get; set; }
}
