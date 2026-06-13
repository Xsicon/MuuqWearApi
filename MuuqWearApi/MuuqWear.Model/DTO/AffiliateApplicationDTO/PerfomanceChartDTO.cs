namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

/// <summary>
/// Contains 30 days of performance data for charting
/// </summary>
public class PerformanceChartDTO
{
    public List<DailyPerformanceDTO> DailyStats { get; set; } = new();
}
