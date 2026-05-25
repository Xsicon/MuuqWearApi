using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models;

[Table("affiliate_clicks")]
public class AffiliateClick : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("affiliate_code")]
    public string AffiliateCode { get; set; } = string.Empty;

    [Column("clicked_at")]
    public DateTime ClickedAt { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("referrer_url")]
    public string? ReferrerUrl { get; set; }

    [Column("converted")]
    public bool Converted { get; set; } = false;

    [Column("order_id")]
    public Guid? OrderId { get; set; }
}
