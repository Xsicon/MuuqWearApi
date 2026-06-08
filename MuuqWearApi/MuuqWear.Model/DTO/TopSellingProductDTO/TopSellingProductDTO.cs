namespace MuuqWear.Model.DTO.TopSellingProductDTO;

public class TopSellingProductDTO
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}
