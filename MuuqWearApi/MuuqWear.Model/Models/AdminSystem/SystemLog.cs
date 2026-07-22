using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AdminSystem;

[Table("system_logs")]
public class SystemLog : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("logged_at")]
    public DateTime LoggedAt { get; set; }

    [Column("level")]
    public string Level { get; set; } = "Info";

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("source")]
    public string? Source { get; set; }
}
