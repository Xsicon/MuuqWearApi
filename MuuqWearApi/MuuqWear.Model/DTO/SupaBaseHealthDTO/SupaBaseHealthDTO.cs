namespace MuuqWear.Model.DTO.SupaBaseHealthDTO;

public class SupabaseHealthDTO
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}
