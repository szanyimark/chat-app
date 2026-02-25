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
        return new AppConfig
        {
            // Database
            DbHost = configuration["DB_HOST"] ?? "localhost",
            DbPort = configuration["DB_PORT"] ?? "5432",
            DbName = configuration["DB_NAME"] ?? "chatapp",
            DbUser = configuration["DB_USER"] ?? "postgres",
            DbPassword = configuration["DB_PASSWORD"] ?? "postgres",

            // JWT
            JwtKey = configuration["JWT_KEY"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
            JwtIssuer = configuration["JWT_ISSUER"] ?? "ChatApp",
            JwtAudience = configuration["JWT_AUDIENCE"] ?? "ChatApp",
            JwtExpiryMinutes = int.Parse(configuration["JWT_EXPIRY_MINUTES"] ?? "60"),

            // Redis
            RedisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379"
        };
    }
}