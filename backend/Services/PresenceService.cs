using System.Collections.Concurrent;
using System.Timers;
using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Timer = System.Timers.Timer;

namespace ChatApp.Backend.Services;

public interface IPresenceService
{
    Task OnConnectedAsync(Guid userId);
    Task OnDisconnectedAsync(Guid userId);
    Task ReceiveHeartbeatAsync(Guid userId);
    HashSet<Guid> GetOnlineUsers();
    bool IsUserOnline(Guid userId);
}

public class PresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<Guid, DateTime> _lastHeartbeat = new();
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IRedisPubSubService? _redisPubSub;
    private readonly ILogger<PresenceService> _logger;
    private readonly Timer _heartbeatCheckTimer;
    private const int HeartbeatTimeoutSeconds = 120; // 2 minutes

    public PresenceService(IDbContextFactory<AppDbContext> dbContextFactory, IRedisPubSubService? redisPubSub, ILogger<PresenceService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _redisPubSub = redisPubSub;
        _logger = logger;
        
        // Check for stale heartbeats every 30 seconds
        _heartbeatCheckTimer = new Timer(30000);
        _heartbeatCheckTimer.Elapsed += async (s, e) => await CheckStaleHeartbeats();
        _heartbeatCheckTimer.Start();
    }

    public async Task ReceiveHeartbeatAsync(Guid userId)
    {
        var isNewUser = !IsUserOnline(userId);
        _lastHeartbeat[userId] = DateTime.UtcNow;
        
        // If this is a new heartbeat, mark user as online and publish to Redis
        if (isNewUser)
        {
            await UpdateUserPresence(userId, true);
            
            // Publish to Redis
            if (_redisPubSub != null)
            {
                var user = new User { Id = userId, IsOnline = true, LastSeenAt = DateTime.UtcNow };
                await _redisPubSub.PublishUserOnlineAsync(userId, user);
            }
        }
        else
        {
            // Always update LastSeenAt even for existing users
            await UpdateUserPresence(userId, true);
        }
        
        _logger.LogDebug("Received heartbeat from user {UserId}", userId);
    }

    public async Task OnConnectedAsync(Guid userId)
    {
        // Initialize heartbeat on connection
        _lastHeartbeat[userId] = DateTime.UtcNow;
        _logger.LogInformation("User {UserId} connected", userId);

        // Update database
        await UpdateUserPresence(userId, true);

        // Publish to Redis for real-time updates
        if (_redisPubSub != null)
        {
            var user = new User { Id = userId, IsOnline = true, LastSeenAt = DateTime.UtcNow };
            await _redisPubSub.PublishUserOnlineAsync(userId, user);
        }
    }

    public async Task OnDisconnectedAsync(Guid userId)
    {
        _lastHeartbeat.TryRemove(userId, out _);
        _logger.LogInformation("User {UserId} disconnected", userId);

        // Update database
        await UpdateUserPresence(userId, false);

        // Publish to Redis for real-time updates
        if (_redisPubSub != null)
        {
            var user = new User { Id = userId, IsOnline = false, LastSeenAt = DateTime.UtcNow };
            await _redisPubSub.PublishUserOnlineAsync(userId, user);
        }
    }

    public HashSet<Guid> GetOnlineUsers()
    {
        // Return users with recent heartbeats
        var onlineUsers = new HashSet<Guid>();
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _lastHeartbeat)
        {
            if ((now - kvp.Value).TotalSeconds < HeartbeatTimeoutSeconds)
            {
                onlineUsers.Add(kvp.Key);
            }
        }
        
        return onlineUsers;
    }

    public bool IsUserOnline(Guid userId)
    {
        if (_lastHeartbeat.TryGetValue(userId, out var lastHeartbeat))
        {
            // Check if heartbeat is still fresh
            if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds < HeartbeatTimeoutSeconds)
            {
                return true;
            }
            
            // Heartbeat is stale, remove it
            _lastHeartbeat.TryRemove(userId, out _);
        }
        
        return false;
    }

    private async Task CheckStaleHeartbeats()
    {
        try
        {
            var staleUsers = new List<Guid>();
            var now = DateTime.UtcNow;

            foreach (var kvp in _lastHeartbeat)
            {
                if ((now - kvp.Value).TotalSeconds >= HeartbeatTimeoutSeconds)
                {
                    staleUsers.Add(kvp.Key);
                }
            }

            foreach (var userId in staleUsers)
            {
                _lastHeartbeat.TryRemove(userId, out _);
                _logger.LogInformation("User {UserId} marked as offline (heartbeat timeout)", userId);

                // Update database
                await UpdateUserPresence(userId, false);

                // Publish to Redis
                if (_redisPubSub != null)
                {
                    var user = new User { Id = userId, IsOnline = false, LastSeenAt = DateTime.UtcNow };
                    await _redisPubSub.PublishUserOnlineAsync(userId, user);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stale heartbeats");
        }
    }

    private async Task UpdateUserPresence(Guid userId, bool isOnline)
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var user = await db.Users.FindAsync(userId);
            
            if (user != null)
            {
                user.IsOnline = isOnline;
                user.LastSeenAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                _logger.LogInformation("User {UserId} presence updated to {IsOnline} in database", userId, isOnline);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user presence in database");
        }
    }
}