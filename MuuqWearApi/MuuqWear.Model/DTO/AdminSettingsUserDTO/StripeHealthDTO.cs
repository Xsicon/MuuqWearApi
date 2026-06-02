namespace MuuqWear.Model.DTO.AdminSettingsUserDTO;

public class StripeHealthDTO
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}
