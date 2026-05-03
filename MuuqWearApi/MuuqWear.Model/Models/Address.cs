using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("addresses")]
public class Address : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("street1")]
    public string Street1 { get; set; } = string.Empty;

    [Column("street2")]
    public string? Street2 { get; set; }

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("state")]
    public string? State { get; set; }

    [Column("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [Column("country")]
    public string Country { get; set; } = "US";

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
