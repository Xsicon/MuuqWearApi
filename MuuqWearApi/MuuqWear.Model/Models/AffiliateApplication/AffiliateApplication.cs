using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AffiliateApplication;

[Table("affiliate_applications")]
public class AffiliateApplication : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("social_handles")]
    public List<SocialHandle> SocialHandles { get; set; } = new();

    [Column("audience_size")]
    public int AudienceSize { get; set; }

    [Column("content_niche")]
    public string ContentNiche { get; set; } = string.Empty;

    [Column("portfolio_url")]
    public string? PortfolioUrl { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("reviewed_by")]
    public Guid? ReviewedBy { get; set; }

    [Column("admin_notes")]
    public string? AdminNotes { get; set; }
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;
    [Column("why_muuqwear")]
    public string? WhyMuuqwear { get; set; }

    [Column("sample_files")]
    public List<string>? SampleFiles { get; set; }

}

// Nested class for social handles
public class SocialHandle
{
    public string Platform { get; set; } = string.Empty; // Instagram, TikTok, YouTube
    public string Handle { get; set; } = string.Empty;   // @username
    public int Followers { get; set; }
}
