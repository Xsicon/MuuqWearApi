namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class TrackClickRequestDTO
{
    public string AffiliateCode { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string ReferrerUrl { get; set; }
}
