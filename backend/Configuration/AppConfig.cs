using System.IO;
using DotNetEnv;

namespace ChatApp.Backend.Configuration;

public class AppConfig
{
    // Database
    public string DbHost { get; set; } = "localhost";
    public string DbPort { get; set; } = "5432";
    public string DbName { get; set; } = "chatapp";
    public string DbUser { get; set; } = "postgres";
    public string DbPassword { get; set; } = "postgres";

    // JWT
    public string JwtKey { get; set; } = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    public string JwtIssuer { get; set; } = "ChatApp";
    public string JwtAudience { get; set; } = "ChatApp";
    public int JwtExpiryMinutes { get; set; } = 60;

    // Redis
    public string RedisConnectionString { get; set; } = "localhost:6379";

    public string GetConnectionString()
    {
        return $"Host={DbHost};Port={DbPort};Database={DbName};Username={DbUser};Password={DbPassword}";
    }
}

public static class ConfigLoader
{
    public static AppConfig Load(IConfiguration configuration)
    {
        // Load .env file from project root (for local development)
        var envPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".env");
        if (System.IO.File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        
        // Helper to get config value - checks environment variables first (docker-compose),
        // then configuration (from .env file), then falls back to default
        string GetValue(string envVar, string configKey, string defaultValue)
        {
            // First check environment variable (set by docker-compose as Database__Host format)
            var envValue = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(envValue))
                return envValue;
                
            // Also check the double-underscore format used by docker-compose
            var dockerFormat = envVar.Replace("_", "__");
            envValue = Environment.GetEnvironmentVariable(dockerFormat);
            if (!string.IsNullOrEmpty(envValue))
                return envValue;
                
            // Then check configuration (from .env file loaded by DotNetEnv)
            var configValue = configuration[configKey];
            if (!string.IsNullOrEmpty(configValue))
                return configValue;
                
            return defaultValue;
        }
        
        return new AppConfig
        {
            // Database
            DbHost = GetValue("DB_HOST", "DB_HOST", "postgres"),
            DbPort = GetValue("DB_PORT", "DB_PORT", "5432"),
            DbName = GetValue("DB_NAME", "DB_NAME", "chatapp"),
            DbUser = GetValue("DB_USER", "DB_USER", "chatapp"),
            DbPassword = GetValue("DB_PASSWORD", "DB_PASSWORD", "chatapp_password"),

            // JWT
            JwtKey = GetValue("JWT_KEY", "JWT_KEY", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"),
            JwtIssuer = GetValue("JWT_ISSUER", "JWT_ISSUER", "ChatApp"),
            JwtAudience = GetValue("JWT_AUDIENCE", "JWT_AUDIENCE", "ChatApp"),
            JwtExpiryMinutes = int.Parse(GetValue("JWT_EXPIRY_MINUTES", "JWT_EXPIRY_MINUTES", "60")),

            // Redis
            RedisConnectionString = GetValue("REDIS_CONNECTION_STRING", "REDIS_CONNECTION_STRING", "redis:6379")
        };
    }
}