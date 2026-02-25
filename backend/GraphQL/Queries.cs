using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

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

    public async Task<User?> GetUserById(Guid id, [Service] AppDbContext db)
    {
        return await db.Users.FindAsync(id);
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

    public async Task<Conversation?> GetConversationById(Guid id, [Service] AppDbContext db)
    {
        return await db.Conversations.FindAsync(id);
    }
}