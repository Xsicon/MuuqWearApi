namespace MuuqWear.Model.DTO.AdminSystem;

public class SyncJobResultModel
{
    public Guid Id { get; set; }
    public string JobKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? RecordsAffected { get; set; }
}
