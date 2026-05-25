namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class DailyPerformanceDTO
{
    public int Day { get; set; }           // Day number (1-30)
    public DateTime Date { get; set; }     // Actual date (e.g., 2024-12-01)
    public int Clicks { get; set; }        // Number of clicks that day
    public int Conversions { get; set; }   // Number of conversions that day
}
