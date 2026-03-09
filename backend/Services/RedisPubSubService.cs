using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatApp.Backend.Models;
using StackExchange.Redis;

namespace ChatApp.Backend.Services;

public interface IRedisPubSubService
{
    Task PublishMessageAsync(Guid conversationId, Message message);
    Task PublishUserOnlineAsync(Guid userId, User user);
    Task PublishFriendRequestUpdatedAsync(Guid userId, FriendRequest friendRequest);
    IAsyncEnumerable<Message> SubscribeToMessages(Guid conversationId, CancellationToken cancellationToken);
    IAsyncEnumerable<User> SubscribeToUserOnline(Guid userId, CancellationToken cancellationToken);
    IAsyncEnumerable<FriendRequest> SubscribeToFriendRequestUpdates(Guid userId, CancellationToken cancellationToken);
}

public class RedisPubSubService : IRedisPubSubService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPubSubService> _logger;

    // JSON serialization options - ignore circular references and null values
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisPubSubService(IConnectionMultiplexer redis, ILogger<RedisPubSubService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishMessageAsync(Guid conversationId, Message message)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            var channel = $"chat:messages:{conversationId}";
            var json = JsonSerializer.Serialize(message, JsonOptions);
            await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
            _logger.LogInformation("Published message to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Redis");
        }
    }

    public async Task PublishUserOnlineAsync(Guid userId, User user)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            var channel = $"chat:online:{userId}";
            var json = JsonSerializer.Serialize(user, JsonOptions);
            await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
            _logger.LogInformation("Published user online status to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish user online status to Redis");
        }
    }

    public async Task PublishFriendRequestUpdatedAsync(Guid userId, FriendRequest friendRequest)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            var channel = $"chat:friend-requests:{userId}";
            var json = JsonSerializer.Serialize(friendRequest, JsonOptions);
            _logger.LogInformation("Publishing friend request to Redis - Channel: {Channel}, Request ID: {RequestId}, From: {FromUserId}, To: {ToUserId}, JSON: {Json}", 
                channel, friendRequest.Id, friendRequest.FromUserId, friendRequest.ToUserId, json);
            await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
            _logger.LogInformation("Successfully published friend request update to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish friend request update to Redis for user {UserId}", userId);
        }
    }

    public IAsyncEnumerable<Message> SubscribeToMessages(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return SubscribeToChannel<Message>($"chat:messages:{conversationId}", cancellationToken);
    }

    public IAsyncEnumerable<User> SubscribeToUserOnline(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return SubscribeToChannel<User>($"chat:online:{userId}", cancellationToken);
    }

    public IAsyncEnumerable<FriendRequest> SubscribeToFriendRequestUpdates(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return SubscribeToChannel<FriendRequest>($"chat:friend-requests:{userId}", cancellationToken);
    }

    private async IAsyncEnumerable<T> SubscribeToChannel<T>(
        string channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelName = RedisChannel.Literal(channel);
        var subscriber = _redis.GetSubscriber();
        var messageQueue = System.Threading.Channels.Channel.CreateUnbounded<RedisValue>();

        // Subscribe once and keep the subscription open
        await subscriber.SubscribeAsync(channelName, (redisChannel, value) =>
        {
            _logger.LogInformation("Received message on Redis channel {Channel}: {Value}", channel, value);
            messageQueue.Writer.TryWrite(value);
        });

        _logger.LogInformation("Subscribed to Redis channel {Channel}", channel);

        try
        {
            // Read messages from the queue as they arrive
            await foreach (var value in messageQueue.Reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogInformation("Processing message from queue for channel {Channel}: {Value}", channel, value);
                if (!value.IsNullOrEmpty)
                {
                    // Deserialize in a separate block to avoid yield in try-catch
                    T? item = DeserializeMessage<T>(value!, channel);

                    if (item != null)
                    {
                        _logger.LogInformation("Yielding deserialized item from channel {Channel}", channel);
                        yield return item;
                    }
                    else
                    {
                        _logger.LogWarning("Deserialized item was null for channel {Channel}", channel);
                    }
                }
            }
        }
        finally
        {
            // Unsubscribe only when the subscription is cancelled
            await subscriber.UnsubscribeAsync(channelName);
            _logger.LogInformation("Unsubscribed from Redis channel {Channel}", channel);
        }
    }

    private T? DeserializeMessage<T>(string value, string channel)
    {
        try
        {
            var item = JsonSerializer.Deserialize<T>(value, JsonOptions);
            _logger.LogInformation("Successfully deserialized message for channel {Channel}", channel);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message from Redis channel {Channel}. Raw value: {Value}", channel, value);
            return default;
        }
    }
}