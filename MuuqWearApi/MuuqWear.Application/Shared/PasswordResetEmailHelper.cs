using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Builds the password-reset email and calls Supabase Auth Admin generate_link.
/// </summary>
public static class PasswordResetEmailHelper
{
    public const string GenericSuccessMessage =
        "If an account exists for that email, a password reset link has been sent.";

    public static (string Subject, string Html, string Text) BuildEmail(string actionLink)
    {
        const string subject = "Reset your password";
        var text =
            "Reset your MuuqWear password using this link:\n\n" +
            actionLink +
            "\n\nThis link expires soon. If you did not request a reset, you can ignore this email.";

        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.5;color:#1E2A47;max-width:560px;margin:0 auto;padding:24px;">
              <h2 style="margin:0 0 12px;font-size:22px;">Reset your password</h2>
              <p style="margin:0 0 20px;">We received a request to reset your MuuqWear password. Click the button below to choose a new one.</p>
              <p style="margin:0 0 24px;">
                <a href="{WebUtility.HtmlEncode(actionLink)}"
                   style="display:inline-block;background:#1E2A47;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:600;">
                  Reset password
                </a>
              </p>
              <p style="margin:0 0 8px;font-size:13px;color:#4A5C7A;">This link expires soon. If the button does not work, copy and paste this URL into your browser:</p>
              <p style="margin:0 0 20px;font-size:12px;word-break:break-all;color:#4A5C7A;">{WebUtility.HtmlEncode(actionLink)}</p>
              <p style="margin:0;font-size:12px;color:#4A5C7A;">If you did not request this, you can safely ignore this email.</p>
            </div>
            """;

        return (subject, html, text);
    }

    public static async Task<string?> GenerateRecoveryLinkAsync(
        HttpClient http,
        string supabaseUrl,
        string serviceRoleKey,
        string email,
        string redirectTo,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = supabaseUrl.TrimEnd('/');
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/auth/v1/admin/generate_link");

        request.Headers.Add("apikey", serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                type = "recovery",
                email,
                redirect_to = redirectTo
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("action_link", out var link)
            && link.ValueKind == JsonValueKind.String)
        {
            var value = link.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // Some responses nest properties under "properties".
        if (document.RootElement.TryGetProperty("properties", out var props)
            && props.TryGetProperty("action_link", out var nested)
            && nested.ValueKind == JsonValueKind.String)
        {
            var value = nested.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
