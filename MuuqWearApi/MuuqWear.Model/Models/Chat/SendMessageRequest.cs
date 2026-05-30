namespace MuuqWear.Model.Models.Chat;
public class SendMessageRequest
{
    public Guid? SessionId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? GuestName { get; set; }

    public string? GuestEmail { get; set; }
}
