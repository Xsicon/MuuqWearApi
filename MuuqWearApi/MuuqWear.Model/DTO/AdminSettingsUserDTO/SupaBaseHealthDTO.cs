namespace MuuqWear.Model.DTO.AdminSettingsUserDTO;

public class SupabaseHealthDTO
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}
