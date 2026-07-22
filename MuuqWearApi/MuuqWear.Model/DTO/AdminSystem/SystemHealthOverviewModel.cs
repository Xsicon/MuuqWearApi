namespace MuuqWear.Model.DTO.AdminSystem;

public class SystemHealthOverviewModel
{
    public bool DatabaseIsHealthy { get; set; }
    public string DatabaseStatus { get; set; } = string.Empty;
    public bool StripeIsHealthy { get; set; }
    public string StripeStatus { get; set; } = string.Empty;
    public bool SupabaseIsHealthy { get; set; }
    public string SupabaseStatus { get; set; } = string.Empty;
    public DateTime? LastBackupAt { get; set; }
    public string LastBackupDisplay { get; set; } = string.Empty;
    public int? ActiveUsersCount { get; set; }
    public DateTime CheckedAt { get; set; }
}
