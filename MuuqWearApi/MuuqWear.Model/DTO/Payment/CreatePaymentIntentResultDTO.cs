namespace MuuqWear.Model.DTO.Payment;

public class CreatePaymentIntentResultDTO
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}
