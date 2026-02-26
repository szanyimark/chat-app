using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChatApp.Backend.Tests;

public class PresenceServiceTests
{
    // Factory to create fresh DbContext for each test
    private IDbContextFactory<AppDbContext> CreateDbContextFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContextFactory(options);
    }

    private PresenceService CreatePresenceService(IDbContextFactory<AppDbContext> factory)
    {
        var logger = new Mock<ILogger<PresenceService>>();
        return new PresenceService(factory, null, logger.Object);
    }

    [Fact]
    public async Task ReceiveHeartbeatAsync_NewUser_ShouldMarkAsOnline()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        await using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            var user = new User { Id = userId, Email = "test@test.com", Username = "test", Password = "hash" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        await presenceService.ReceiveHeartbeatAsync(userId);

        // Assert
        await using var verifyDb = await dbContextFactory.CreateDbContextAsync();
        var updatedUser = await verifyDb.Users.FindAsync(userId);
        Assert.True(updatedUser?.IsOnline);
    }

    [Fact]
    public async Task ReceiveHeartbeatAsync_ExistingHeartbeat_ShouldNotDuplicatePublish()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        await using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            var user = new User { Id = userId, Email = "test@test.com", Username = "test", Password = "hash", IsOnline = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act - Send heartbeat when user is already online
        await presenceService.ReceiveHeartbeatAsync(userId);

        // Assert - User should still be online
        await using var verifyDb = await dbContextFactory.CreateDbContextAsync();
        var updatedUser = await verifyDb.Users.FindAsync(userId);
        Assert.True(updatedUser?.IsOnline);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldMarkUserAsOnline()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        await using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            var user = new User { Id = userId, Email = "test@test.com", Username = "test", Password = "hash" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        await presenceService.OnConnectedAsync(userId);

        // Assert
        await using var verifyDb = await dbContextFactory.CreateDbContextAsync();
        var updatedUser = await verifyDb.Users.FindAsync(userId);
        Assert.True(updatedUser?.IsOnline);
        Assert.NotNull(updatedUser?.LastSeenAt);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldMarkUserAsOffline()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        await using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            var user = new User { Id = userId, Email = "test@test.com", Username = "test", Password = "hash", IsOnline = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        await presenceService.OnDisconnectedAsync(userId);

        // Assert
        await using var verifyDb = await dbContextFactory.CreateDbContextAsync();
        var updatedUser = await verifyDb.Users.FindAsync(userId);
        Assert.False(updatedUser?.IsOnline);
    }

    [Fact]
    public void IsUserOnline_WithNoHeartbeat_ShouldReturnFalse()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        // Act
        var isOnline = presenceService.IsUserOnline(userId);
        
        // Assert
        Assert.False(isOnline);
    }

    [Fact]
    public void GetOnlineUsers_EmptyDictionary_ShouldReturnEmptySet()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        
        // Act
        var onlineUsers = presenceService.GetOnlineUsers();

        // Assert
        Assert.NotNull(onlineUsers);
        Assert.Empty(onlineUsers);
    }

    [Fact]
    public async Task ReceiveHeartbeatAsync_UpdatesLastSeenAt()
    {
        // Arrange
        var dbContextFactory = CreateDbContextFactory();
        var presenceService = CreatePresenceService(dbContextFactory);
        var userId = Guid.NewGuid();
        
        await using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            var user = new User { Id = userId, Email = "test@test.com", Username = "test", Password = "hash" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        await presenceService.ReceiveHeartbeatAsync(userId);

        // Assert
        await using var verifyDb = await dbContextFactory.CreateDbContextAsync();
        var updatedUser = await verifyDb.Users.FindAsync(userId);
        Assert.NotNull(updatedUser?.LastSeenAt);
    }
}

// Factory that creates new AppDbContext instances
public class AppDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppDbContext(_options));
    }
}