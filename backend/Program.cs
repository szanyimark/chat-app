using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotChocolate.AspNetCore;
using HotChocolate.Data;
using ChatApp.Backend.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ChatApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ChatApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddFiltering()
    .AddSorting()
    .AddInMemorySubscriptions();

// Configure Redis for pub/sub (optional - using in-memory for now)
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        return ConnectionMultiplexer.Connect(redisConnectionString);
    }
    catch
    {
        // Redis not available, return a dummy connection
        return null!;
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Map GraphQL endpoint
app.MapGraphQL("/graphql")
    .WithOptions(new GraphQLServerOptions
    {
        Tool = { Enable = app.Environment.IsDevelopment() }
    });

// Map GraphQL WebSocket endpoint for subscriptions
app.MapGraphQLWebSocket("/graphql");

app.Run();

// Placeholder Query type
public class Query
{
    public string Hello() => "Hello from GraphQL!";
}

// Placeholder Mutation type
public class Mutation
{
    public string Hello() => "Mutation works!";
}

// Placeholder Subscription type
public class Subscription
{
    public string Hello() => "Subscription works!";
}
