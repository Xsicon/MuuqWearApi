using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AdminSystem;

[Table("system_metadata")]
public class SystemMetadata : BaseModel
{
    [PrimaryKey("key", false)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
