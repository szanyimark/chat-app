using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotChocolate.AspNetCore;
using ChatApp.Backend.Configuration;
using ChatApp.Backend.Data;
using ChatApp.Backend.GraphQL;
using ChatApp.Backend.GraphQL.Types;
using ChatApp.Backend.Services;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var appConfig = ConfigLoader.Load(builder.Configuration);

// Add services to the container.

// Configure Entity Framework Core with PostgreSQL
// Use AddDbContextFactory instead of AddDbContext to avoid the scoped/singleton conflict
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(appConfig.GetConnectionString()));

// Register AppConfig
builder.Services.AddSingleton(appConfig);

// Register JwtService
builder.Services.AddSingleton<IJwtService, JwtService>();

// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = appConfig.JwtIssuer,
            ValidAudience = appConfig.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfig.JwtKey))
        };
        
        // Add events to check token blacklist
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenBlacklist = context.HttpContext.RequestServices.GetService<ITokenBlacklistService>();
                if (tokenBlacklist != null)
                {
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (await tokenBlacklist.IsBlacklistedAsync(token))
                        {
                            context.Fail("Token has been revoked");
                        }
                    }
                }
                await Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddType<UserType>()
    .AddType<ConversationGraphType>()
    .AddType<MessageType>()
    .AddType<ConversationMemberType>()
    .AddType<ConversationTypeEnum>()
    .AddType<MemberRoleEnum>()
    .AddFiltering()
    .AddSorting()
    .AddInMemorySubscriptions();

// Configure Redis for pub/sub (abortConnect=false allows app to start if Redis isn't ready yet)
var redisOptions = ConfigurationOptions.Parse(appConfig.RedisConnectionString);
redisOptions.AbortOnConnectFail = false;
var redisConnection = ConnectionMultiplexer.Connect(redisOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// Register Redis Pub/Sub Service
builder.Services.AddSingleton<IRedisPubSubService, RedisPubSubService>();

// Register Token Blacklist Service
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// Register Presence Service
builder.Services.AddSingleton<IPresenceService, PresenceService>();

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
