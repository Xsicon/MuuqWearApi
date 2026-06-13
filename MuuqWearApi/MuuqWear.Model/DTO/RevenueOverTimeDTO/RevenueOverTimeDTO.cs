namespace MuuqWear.Model.DTO.RevenueOverTimeDTO;

public class RevenueOverTimeDTO
{
    public List<DailyRevenueDTO> DailyRevenue { get; set; } = new();
    public decimal CurrentTotal { get; set; }
    public decimal PreviousTotal { get; set; }

    public decimal PercentChange =>
        PreviousTotal == 0 ? 0 :
        Math.Round(((CurrentTotal - PreviousTotal) / PreviousTotal) * 100, 1);

    public bool IsUp => CurrentTotal >= PreviousTotal;
}

public class DailyRevenueDTO
{
    public DateTime Day { get; set; }
    public decimal Revenue { get; set; }
}
