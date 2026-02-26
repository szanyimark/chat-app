using System.Security.Cryptography;
using System.Text;
using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Backend.GraphQL;

public class Mutation
{
    // Auth - Register
    public async Task<AuthPayload> Register(RegisterInput input, [Service] AppDbContext db, [Service] IJwtService jwtService)
    {
        // Check if user already exists
        if (await db.Users.AnyAsync(u => u.Email == input.Email))
            throw new GraphQLException("Email already registered");

        if (await db.Users.AnyAsync(u => u.Username == input.Username))
            throw new GraphQLException("Username already taken");

        // Hash password
        var passwordHash = HashPassword(input.Password);

        var user = new User
        {
            Email = input.Email,
            Username = input.Username,
            Password = passwordHash
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Generate real JWT token
        var token = jwtService.GenerateToken(user);

        return new AuthPayload(user, token);
    }

    // Auth - Login
    public async Task<AuthPayload> Login(LoginInput input, [Service] AppDbContext db, [Service] IJwtService jwtService, [Service] IRedisPubSubService? redisPubSub)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == input.Email);
        if (user == null)
            throw new GraphQLException("Invalid credentials");

        var passwordHash = HashPassword(input.Password);
        if (user.Password != passwordHash)
            throw new GraphQLException("Invalid credentials");

        // Set user as online
        user.IsOnline = true;
        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Publish online status to Redis
        if (redisPubSub != null)
        {
            await redisPubSub.PublishUserOnlineAsync(user.Id, user);
        }

        // Generate real JWT token
        var token = jwtService.GenerateToken(user);

        return new AuthPayload(user, token);
    }

    // Auth - Logout
    public async Task<bool> Logout([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] IRedisPubSubService? redisPubSub, [Service] ITokenBlacklistService? tokenBlacklist, [Service] IJwtService? jwtService)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new GraphQLException("Not authenticated");

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return false;

        // Set user as offline
        user.IsOnline = false;
        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Publish offline status to Redis for presence tracking
        if (redisPubSub != null)
        {
            await redisPubSub.PublishUserOnlineAsync(user.Id, user);
        }

        // Get the token from the Authorization header and add to blacklist
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring(7);
            if (tokenBlacklist != null && jwtService != null)
            {
                // Get token expiry from JWT
                var expiry = jwtService.GetTokenExpiry(token);
                var expiryMinutes = expiry.HasValue
                    ? (expiry.Value - DateTime.UtcNow).TotalMinutes
                    : 60;
                
                // Ensure at least 1 minute remaining
                if (expiryMinutes < 1)
                    expiryMinutes = 1;
                
                await tokenBlacklist.AddToBlacklistAsync(token, TimeSpan.FromMinutes(expiryMinutes));
            }
        }

        return true;
    }

    // Create Conversation
    public async Task<Conversation> CreateConversation(CreateConversationInput input, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new GraphQLException("Not authenticated");

        var conversation = new Conversation
        {
            Type = input.Type,
            Name = input.Name
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        // Add creator as member
        var member = new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = userId,
            Role = MemberRole.Admin
        };

        db.ConversationMembers.Add(member);
        await db.SaveChangesAsync();

        return conversation;
    }

    // Join Conversation
    public async Task<Conversation> JoinConversation(Guid id, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new GraphQLException("Not authenticated");

        var conversation = await db.Conversations.FindAsync(id);
        if (conversation == null)
            throw new GraphQLException("Conversation not found");

        // Check if already a member
        var existingMember = await db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == id && cm.UserId == userId);

        if (existingMember != null)
            throw new GraphQLException("Already a member");

        var member = new ConversationMember
        {
            ConversationId = id,
            UserId = userId,
            Role = MemberRole.Member
        };

        db.ConversationMembers.Add(member);
        await db.SaveChangesAsync();

        return conversation;
    }

    // Send Message
    public async Task<Message> SendMessage(SendMessageInput input, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] IRedisPubSubService? redisPubSub)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new GraphQLException("Not authenticated");

        var message = new Message
        {
            ConversationId = input.ConversationId,
            SenderId = userId,
            Content = input.Content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Load related data for return
        await db.Entry(message).Reference(m => m.Sender).LoadAsync();
        await db.Entry(message).Reference(m => m.Conversation).LoadAsync();

        // Publish to Redis for real-time subscriptions
        if (redisPubSub != null)
        {
            await redisPubSub.PublishMessageAsync(input.ConversationId, message);
        }

        return message;
    }

    // Heartbeat - client calls this every 30 seconds to stay online
    public async Task<bool> Heartbeat([Service] IHttpContextAccessor httpContextAccessor, [Service] IPresenceService? presenceService)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return false;

        if (presenceService != null)
        {
            await presenceService.ReceiveHeartbeatAsync(userId);
        }
        
        return true;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

// Input types
public record RegisterInput(string Email, string Username, string Password);
public record LoginInput(string Email, string Password);
public record CreateConversationInput(ConversationType Type, string? Name);
public record SendMessageInput(Guid ConversationId, string Content);

// Output types
public class AuthPayload
{
    public User User { get; }
    public string Token { get; }

    public AuthPayload(User user, string token)
    {
        User = user;
        Token = token;
    }
}