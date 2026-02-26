using ChatApp.Backend.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace ChatApp.Backend.Tests;

public class TokenBlacklistServiceTests
{
    [Fact]
    public async Task IsBlacklistedAsync_ShouldReturnTrue_WhenTokenIsBlacklisted()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        var result = await service.IsBlacklistedAsync("test-token");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsBlacklistedAsync_ShouldReturnFalse_WhenTokenIsNotBlacklisted()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        var result = await service.IsBlacklistedAsync("test-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsBlacklistedAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        var result = await service.IsBlacklistedAsync("test-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsBlacklistedAsync_ShouldCallStringGetAsync()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        await service.IsBlacklistedAsync("test-token");

        // Assert
        mockDatabase.Verify(
            x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task AddToBlacklistAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        
        // Setup to accept any call
        mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        var service = new TokenBlacklistService(mockRedis.Object);
        var token = "test-token";
        var expiry = TimeSpan.FromHours(1);

        // Act & Assert - should not throw
        await service.AddToBlacklistAsync(token, expiry);
    }

    [Fact]
    public async Task AddToBlacklistAsync_ShouldWorkWithShortExpiry()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        
        mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        var service = new TokenBlacklistService(mockRedis.Object);
        var token = "short-lived-token";
        var expiry = TimeSpan.FromMinutes(5);

        // Act & Assert - should not throw
        await service.AddToBlacklistAsync(token, expiry);
    }

    [Fact]
    public async Task AddToBlacklistAsync_ShouldWorkWithLongExpiry()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        
        mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        var service = new TokenBlacklistService(mockRedis.Object);
        var token = "long-lived-token";
        var expiry = TimeSpan.FromDays(30);

        // Act & Assert - should not throw
        await service.AddToBlacklistAsync(token, expiry);
    }

    [Fact]
    public async Task IsBlacklistedAsync_ShouldWorkWithDifferentTokens()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        
        // First call returns true, second call returns false
        var callCount = 0;
        mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.FromResult(callCount++ == 0 ? (RedisValue)true : (RedisValue)false));

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        var result1 = await service.IsBlacklistedAsync("token1");
        var result2 = await service.IsBlacklistedAsync("token2");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task AddToBlacklistAsync_ShouldCallGetDatabase()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        
        mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        var service = new TokenBlacklistService(mockRedis.Object);

        // Act
        await service.AddToBlacklistAsync("test-token", TimeSpan.FromHours(1));

        // Assert
        mockRedis.Verify(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()), Times.Once);
    }
}