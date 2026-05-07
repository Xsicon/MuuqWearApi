namespace MuuqWear.Model.DTO.OrderDTO;

public class BulkUpdateOrderStatusDTO
{
    public List<Guid> OrderIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
