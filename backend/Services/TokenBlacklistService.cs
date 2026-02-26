using StackExchange.Redis;

namespace ChatApp.Backend.Services;

public interface ITokenBlacklistService
{
    Task AddToBlacklistAsync(string token, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string token);
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private const string BlacklistPrefix = "token:blacklist:";

    public TokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task AddToBlacklistAsync(string token, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var key = BlacklistPrefix + token.GetHashCode();
        await db.StringSetAsync(key, true, expiry);
    }

    public async Task<bool> IsBlacklistedAsync(string token)
    {
        var db = _redis.GetDatabase();
        var key = BlacklistPrefix + token.GetHashCode();
        return await db.StringGetAsync(key) == true;
    }
}