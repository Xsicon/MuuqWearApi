using Microsoft.Extensions.Configuration;
using Supabase;

namespace MuuqWear.Application.Shared;

public class SupabaseAdminClientFactory
{
    private readonly IConfiguration _configuration;

    public SupabaseAdminClientFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Service role key bypasses RLS — admin operations only.
    // Never forwards the caller JWT (unlike SupabaseClientFactory).
    public Client CreateClient()
    {
        var url = _configuration["SupaBase:Url"]!;
        var serviceRoleKey = _configuration["SupaBase:ServiceRoleKey"]
            ?? _configuration["Supabase:ServiceRoleKey"]!;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false,
            Schema = "MuuqWear"
        };

        return new Client(url, serviceRoleKey, options);
    }
}
