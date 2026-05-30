using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Profiles;
[Table("profiles")]
public class Profiles : BaseModel
{
    [PrimaryKey("id", true)]
    public Guid? Id { get; set; }
    [Column("full_name")]
    public string? FullName { get; set; }
    [Column("phone")]
    public string? Phone { get; set; }
    [Column("role")]
    public string? Role { get; set; }
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    [Column("email")]
    public string? Email { get; set; }
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    [Column("last_active_at")]
    public DateTime? LastActiveAt { get; set; }
    [Column("notifications_read_at")]
    public DateTime? NotificationsReadAt { get; set; }
    [Column("affiliate_tier")]
    public string AffiliateTier { get; set; } = "none";

    [Column("affiliate_items_sold")]
    public int AffiliateItemsSold { get; set; }

    [Column("affiliate_commission_earned")]
    public decimal AffiliateCommissionEarned { get; set; }

    [Column("affiliate_bonus_earned")]
    public decimal AffiliateBonusEarned { get; set; }

    [Column("affiliate_application_status")]
    public string AffiliateApplicationStatus { get; set; } = "not_applied";
    [Column("affiliate_code")]
    public string? AffiliateCode { get; set; } = string.Empty;
    [Column("affiliate_total_clicks")]
    public int AffiliateTotalClicks { get; set; } = 0;
}
