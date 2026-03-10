using System.Security.Cryptography;
using System.Text;
using System.Linq;
using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using Microsoft.AspNetCore.Http;
using ChatApp.Backend.Services;
using HotChocolate;
using HotChocolate.Subscriptions;
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

        // Generate tag and ensure uniqueness
        var baseTag = GenerateUserTag(input.Username);
        var tag = baseTag;
        while (await db.Users.AnyAsync(u => u.Tag == tag))
        {
            tag = $"{baseTag}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        var user = new User
        {
            Email = input.Email,
            Username = input.Username,
            Tag = tag,
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
    public async Task<Conversation> JoinConversation([ID] string id, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new GraphQLException("Not authenticated");

        if (!Guid.TryParse(id, out var parsedConversationId))
            throw new GraphQLException("Invalid conversation ID format");

        var conversation = await db.Conversations.FindAsync(parsedConversationId);
        if (conversation == null)
            throw new GraphQLException("Conversation not found");

        // Check if already a member
        var existingMember = await db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == parsedConversationId && cm.UserId == userId);

        if (existingMember != null)
            throw new GraphQLException("Already a member");

        var member = new ConversationMember
        {
            ConversationId = parsedConversationId,
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

        if (!Guid.TryParse(input.ConversationId, out var parsedConversationId))
            throw new GraphQLException("Invalid conversation ID format");

        var message = new Message
        {
            ConversationId = parsedConversationId,
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
            await redisPubSub.PublishMessageAsync(parsedConversationId, message);
        }

        return message;
    }

    // Friends - Send request
    public async Task<FriendRequest> SendFriendRequest([ID] string userId, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] IRedisPubSubService? redisPubSub, [Service] ITopicEventSender topicEventSender)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            throw new GraphQLException("Not authenticated");

        if (!Guid.TryParse(userId, out var targetUserId))
            throw new GraphQLException("Invalid user ID format");

        if (meId == targetUserId)
            throw new GraphQLException("Cannot friend yourself");

        // Ensure target exists
        var targetExists = await db.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetExists)
            throw new GraphQLException("User not found");

        // Already friends?
        var alreadyFriends = await db.Friendships.AnyAsync(f => f.UserId == meId && f.FriendId == targetUserId);
        if (alreadyFriends)
            throw new GraphQLException("Already friends");

        // Existing request either direction?
        var existing = await db.FriendRequests
            .FirstOrDefaultAsync(fr =>
                (fr.FromUserId == meId && fr.ToUserId == targetUserId) ||
                (fr.FromUserId == targetUserId && fr.ToUserId == meId));

        if (existing != null)
        {
            if (existing.Status == FriendRequestStatus.Pending)
                throw new GraphQLException("Friend request already pending");

            // If previously rejected/cancelled, allow re-send by resetting
            existing.FromUserId = meId;
            existing.ToUserId = targetUserId;
            existing.Status = FriendRequestStatus.Pending;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            System.Console.WriteLine($"[DEBUG] Re-sending friend request from {meId} to {targetUserId}");
            await PublishFriendRequestUpdateAsync(existing, redisPubSub, topicEventSender);
            return existing;
        }

        var request = new FriendRequest
        {
            FromUserId = meId,
            ToUserId = targetUserId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.FriendRequests.Add(request);
        await db.SaveChangesAsync();
        System.Console.WriteLine($"[DEBUG] Sending new friend request from {meId} to {targetUserId}");
        await PublishFriendRequestUpdateAsync(request, redisPubSub, topicEventSender);
        return request;
    }

    // Friends - Cancel my outgoing request
    public async Task<bool> CancelFriendRequest([ID] string requestId, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] IRedisPubSubService? redisPubSub, [Service] ITopicEventSender topicEventSender)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            throw new GraphQLException("Not authenticated");

        if (!Guid.TryParse(requestId, out var parsedRequestId))
            throw new GraphQLException("Invalid request ID format");

        var req = await db.FriendRequests.FindAsync(parsedRequestId);
        if (req == null)
            return false;

        if (req.FromUserId != meId)
            throw new GraphQLException("Not allowed");

        if (req.Status != FriendRequestStatus.Pending)
            return false;

        req.Status = FriendRequestStatus.Cancelled;
        req.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await PublishFriendRequestUpdateAsync(req, redisPubSub, topicEventSender);
        return true;
    }

    // Friends - Respond to incoming request
    public async Task<bool> RespondToFriendRequest([ID] string requestId, bool accept, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] IRedisPubSubService? redisPubSub, [Service] ITopicEventSender topicEventSender)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            throw new GraphQLException("Not authenticated");

        if (!Guid.TryParse(requestId, out var parsedRequestId))
            throw new GraphQLException("Invalid request ID format");

        var req = await db.FriendRequests.FindAsync(parsedRequestId);
        if (req == null)
            return false;

        if (req.ToUserId != meId)
            throw new GraphQLException("Not allowed");

        if (req.Status != FriendRequestStatus.Pending)
            return false;

        if (!accept)
        {
            req.Status = FriendRequestStatus.Rejected;
            req.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await PublishFriendRequestUpdateAsync(req, redisPubSub, topicEventSender);
            return true;
        }

        // Accept: mark request + create friendships (two directed edges)
        req.Status = FriendRequestStatus.Accepted;
        req.UpdatedAt = DateTime.UtcNow;

        if (!await db.Friendships.AnyAsync(f => f.UserId == req.FromUserId && f.FriendId == req.ToUserId))
            db.Friendships.Add(new Friendship { UserId = req.FromUserId, FriendId = req.ToUserId });

        if (!await db.Friendships.AnyAsync(f => f.UserId == req.ToUserId && f.FriendId == req.FromUserId))
            db.Friendships.Add(new Friendship { UserId = req.ToUserId, FriendId = req.FromUserId });

        await db.SaveChangesAsync();
        await PublishFriendRequestUpdateAsync(req, redisPubSub, topicEventSender);
        return true;
    }

    private static async Task PublishFriendRequestUpdateAsync(FriendRequest friendRequest, IRedisPubSubService? redisPubSub, ITopicEventSender topicEventSender)
    {
        // Don't load related entities - they have circular references that break JSON serialization.
        // The GraphQL type will resolve them separately from the database.
        // We just need to send the FriendRequest with IDs only.
        var cleanRequest = new FriendRequest
        {
            Id = friendRequest.Id,
            FromUserId = friendRequest.FromUserId,
            ToUserId = friendRequest.ToUserId,
            Status = friendRequest.Status,
            CreatedAt = friendRequest.CreatedAt,
            UpdatedAt = friendRequest.UpdatedAt
            // Note: FromUser and ToUser are intentionally not set to avoid circular references
        };

        // Publish through HotChocolate topic system (used by GraphQL subscriptions).
        await topicEventSender.SendAsync($"friendRequestUpdated_{friendRequest.ToUserId}", cleanRequest);

        // Keep Redis publish for existing diagnostics/integration paths.
        if (redisPubSub != null)
        {
            await redisPubSub.PublishFriendRequestUpdatedAsync(friendRequest.ToUserId, cleanRequest);
        }

        if (friendRequest.FromUserId != friendRequest.ToUserId)
        {
            await topicEventSender.SendAsync($"friendRequestUpdated_{friendRequest.FromUserId}", cleanRequest);
            if (redisPubSub != null)
            {
                await redisPubSub.PublishFriendRequestUpdatedAsync(friendRequest.FromUserId, cleanRequest);
            }
        }
    }

    // Friends - Remove friend
    public async Task<bool> RemoveFriend([ID] string userId, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db, [Service] ITopicEventSender topicEventSender)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            throw new GraphQLException("Not authenticated");

        if (!Guid.TryParse(userId, out var targetUserId))
            throw new GraphQLException("Invalid user ID format");

        var edges = await db.Friendships
            .Where(f => (f.UserId == meId && f.FriendId == targetUserId) || (f.UserId == targetUserId && f.FriendId == meId))
            .ToListAsync();

        if (edges.Count == 0)
            return false;

        db.Friendships.RemoveRange(edges);
        await db.SaveChangesAsync();

        // Notify both users so their friends list can refresh in real time.
        await topicEventSender.SendAsync($"friendshipUpdated_{meId}", true);
        if (targetUserId != meId)
        {
            await topicEventSender.SendAsync($"friendshipUpdated_{targetUserId}", true);
        }

        return true;
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

    private static string GenerateUserTag(string username)
    {
        // tag format: @username_optionalUUID
        // - always starts with '@'
        // - username is normalized to [a-z0-9_]
        // - if the base tag is already taken, append _ + 8-char UUID suffix
        var normalized = new string(username
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray());

        normalized = string.IsNullOrWhiteSpace(normalized) ? "user" : normalized;

        // NOTE: uniqueness is enforced in Register() by checking the DB.
        // This helper only generates the base; Register() will retry with suffix if needed.
        return $"@{normalized}";
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
public record SendMessageInput(string ConversationId, string Content);

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