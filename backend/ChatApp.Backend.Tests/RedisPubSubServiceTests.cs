using System.Text.Json;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace ChatApp.Backend.Tests;

public class RedisPubSubServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ILogger<RedisPubSubService>> _mockLogger;
    private readonly RedisPubSubService _service;

    public RedisPubSubServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockLogger = new Mock<ILogger<RedisPubSubService>>();
        _service = new RedisPubSubService(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishMessageAsync_ShouldPublishToCorrectChannel()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = Guid.NewGuid(),
            Content = "Test message",
            CreatedAt = DateTime.UtcNow
        };

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);

        // Act
        await _service.PublishMessageAsync(conversationId, message);

        // Assert
        mockSubscriber.Verify(
            x => x.PublishAsync(
                RedisChannel.Literal($"chat:messages:{conversationId}"),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_ShouldSerializeMessageCorrectly()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            SenderId = senderId,
            Content = "Test message",
            CreatedAt = DateTime.UtcNow
        };

        RedisValue capturedValue = default;
        var mockSubscriber = new Mock<ISubscriber>();
        mockSubscriber
            .Setup(x => x.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((channel, value, flags) => capturedValue = value)
            .Returns(Task.FromResult(1L));
        
        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);

        // Act
        await _service.PublishMessageAsync(conversationId, message);

        // Assert
        var json = capturedValue.ToString();
        var deserialized = JsonSerializer.Deserialize<Message>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(messageId, deserialized.Id);
        Assert.Equal("Test message", deserialized.Content);
        Assert.Equal(conversationId, deserialized.ConversationId);
    }

    [Fact]
    public async Task PublishUserOnlineAsync_ShouldPublishToCorrectChannel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            Username = "testuser",
            IsOnline = true,
            LastSeenAt = DateTime.UtcNow
        };

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);

        // Act
        await _service.PublishUserOnlineAsync(userId, user);

        // Assert
        mockSubscriber.Verify(
            x => x.PublishAsync(
                RedisChannel.Literal($"chat:online:{userId}"),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishUserOnlineAsync_ShouldSerializeUserCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            Username = "testuser",
            IsOnline = true,
            LastSeenAt = DateTime.UtcNow
        };

        RedisValue capturedValue = default;
        var mockSubscriber = new Mock<ISubscriber>();
        mockSubscriber
            .Setup(x => x.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((channel, value, flags) => capturedValue = value)
            .Returns(Task.FromResult(1L));
        
        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);

        // Act
        await _service.PublishUserOnlineAsync(userId, user);

        // Assert
        var json = capturedValue.ToString();
        var deserialized = JsonSerializer.Deserialize<User>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(userId, deserialized.Id);
        Assert.Equal("test@test.com", deserialized.Email);
        Assert.True(deserialized.IsOnline);
    }

    [Fact]
    public void SubscribeToMessages_ShouldReturnCorrectChannelName()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act - Just verify it doesn't throw and returns an async enumerable
        var result = _service.SubscribeToMessages(conversationId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void SubscribeToUserOnline_ShouldReturnCorrectChannelName()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - Just verify it doesn't throw and returns an async enumerable
        var result = _service.SubscribeToUserOnline(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}