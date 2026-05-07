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

    //  single responsibility — creates admin client with service role key
    // service role key bypasses RLS → admin operations only 
    // never use this client for user-facing operations 
    public Supabase.Client CreateClient()
    {
        var url = _configuration["SupaBase:Url"]!;
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]!;

        var options = new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false,
            Schema = "MuuqWear"
        };

        return new Supabase.Client(url, serviceRoleKey, options);
    }
}
