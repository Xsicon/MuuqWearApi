namespace MuuqWear.Application.Shared;

public interface IPostmarkEmailSender
{
    /// <summary>
    /// Sends a transactional email via Postmark. Returns false when the send was skipped
    /// (missing config) or Postmark rejected the message.
    /// </summary>
    Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        string tag,
        CancellationToken cancellationToken = default);
}
