using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Supabase;

public class SupabaseClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupabaseClientFactory(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Supabase.Client CreateClient()
    {
        var url = _configuration["SupaBase:Url"]!;
        var key = _configuration["Authentication:SupabaseApiKey"]!;

        var token = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        var options = new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false,
            Schema = "MuuqWear",
            Headers = string.IsNullOrEmpty(token)
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {token}" }
                }
        };

        return new Supabase.Client(url, key, options);
    }
}