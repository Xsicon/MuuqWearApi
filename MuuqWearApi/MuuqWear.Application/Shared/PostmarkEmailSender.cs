using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Postmark transactional sender. Server token must come from secrets / env — never source control.
/// </summary>
public sealed class PostmarkEmailSender : IPostmarkEmailSender
{
    private const string PostmarkEndpoint = "https://api.postmarkapp.com/email";

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostmarkEmailSender> _logger;

    public PostmarkEmailSender(
        HttpClient http,
        IConfiguration configuration,
        ILogger<PostmarkEmailSender> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        string tag,
        CancellationToken cancellationToken = default)
    {
        var serverToken = _configuration["Postmark:ServerToken"];
        var fromEmail = _configuration["Postmark:FromEmail"];
        var messageStream = _configuration["Postmark:MessageStream"] ?? "outbound";

        if (string.IsNullOrWhiteSpace(serverToken) || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning(
                "Postmark not configured (Postmark:ServerToken / Postmark:FromEmail). Email to {Email} was not queued.",
                toEmail);
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, PostmarkEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Postmark-Server-Token", serverToken);
        request.Content = JsonContent.Create(new PostmarkEmailRequest
        {
            From = fromEmail,
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            MessageStream = messageStream,
            Tag = tag
        });

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Postmark send failed for {Email}: {Status} {Body}",
                toEmail,
                (int)response.StatusCode,
                Truncate(body));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Postmark send threw for {Email}", toEmail);
            return false;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400] + "…";

    private sealed class PostmarkEmailRequest
    {
        [JsonPropertyName("From")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("To")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("Subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("HtmlBody")]
        public string HtmlBody { get; set; } = string.Empty;

        [JsonPropertyName("TextBody")]
        public string TextBody { get; set; } = string.Empty;

        [JsonPropertyName("MessageStream")]
        public string MessageStream { get; set; } = "outbound";

        [JsonPropertyName("Tag")]
        public string Tag { get; set; } = string.Empty;
    }
}
