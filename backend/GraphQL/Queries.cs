using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ChatApp.Backend.GraphQL;

public class Query
{
    // Auth
    public async Task<User?> GetMe([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return await db.Users.FindAsync(userId);
    }

    // Users
    public async Task<List<User>> GetUsers([Service] AppDbContext db)
    {
        return await db.Users.ToListAsync();
    }

    public async Task<List<User>> SearchUsers(string? searchTerm, [Service] AppDbContext db)
    {
        var query = db.Users.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.Tag.ToLower().Contains(term));
        }
        
        return await query.ToListAsync();
    }

    // Friends
    public async Task<List<User>> GetMyFriends([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            return new List<User>();

        var friendIds = await db.Friendships
            .Where(f => f.UserId == meId)
            .Select(f => f.FriendId)
            .ToListAsync();

        return await db.Users
            .Where(u => friendIds.Contains(u.Id))
            .ToListAsync();
    }

    public async Task<List<FriendRequest>> GetMyIncomingFriendRequests([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            return new List<FriendRequest>();

        return await db.FriendRequests
            .Where(fr => fr.ToUserId == meId && fr.Status == FriendRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FriendRequest>> GetMyOutgoingFriendRequests([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId))
            return new List<FriendRequest>();

        return await db.FriendRequests
            .Where(fr => fr.FromUserId == meId && fr.Status == FriendRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();
    }

    public async Task<User?> GetUserById([ID] string id, [Service] AppDbContext db)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            return null;
        }

        return await db.Users.FindAsync(parsedId);
    }

    // Conversations
    public async Task<List<Conversation>> GetMyConversations([Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return new List<Conversation>();

        var conversationIds = await db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        return await db.Conversations
            .Where(c => conversationIds.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<Conversation?> GetConversationById([ID] string id, [Service] IHttpContextAccessor httpContextAccessor, [Service] AppDbContext db)
    {
        if (!Guid.TryParse(id, out var parsedId))
        {
            return null;
        }

        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        // Check if user is a member of the conversation
        var isMember = await db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == parsedId && cm.UserId == userId);

        if (!isMember)
            return null;

        return await db.Conversations.FindAsync(parsedId);
    }
}