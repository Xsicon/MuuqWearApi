namespace MuuqWear.Model.DTO.AdminSystem;

public class IntegrationStatusModel
{
    public string Name { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public bool SupportsLiveHealthCheck { get; set; }
    public bool SupportsReconnect { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}
