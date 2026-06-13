using MuuqWear.Model.Models.AffiliateApplication;

namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class SubmitAffiliateApplicationDTO
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<SocialHandle> SocialHandles { get; set; } = new();
    public int AudienceSize { get; set; }
    public string ContentNiche { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string? WhyMuuqwear { get; set; }
    public List<string>? SampleFiles { get; set; }
}




