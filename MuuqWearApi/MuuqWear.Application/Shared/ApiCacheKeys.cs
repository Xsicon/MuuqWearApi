namespace MuuqWear.Application.Shared;

public static class ApiCacheKeys
{
    public const string HomeProducts = "cache:products:home";
    public const string CategoriesAll = "cache:categories:all";
    public const string VoteStats = "cache:vote:stats";

    public static string LastActive(Guid userId) => $"cache:last-active:{userId}";

    public static readonly TimeSpan ReadTtl = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan LastActiveThrottle = TimeSpan.FromMinutes(5);
}
