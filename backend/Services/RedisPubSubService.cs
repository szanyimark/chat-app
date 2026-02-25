using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatApp.Backend.Models;
using StackExchange.Redis;

namespace ChatApp.Backend.Services;

public interface IRedisPubSubService
{
    Task PublishMessageAsync(Guid conversationId, Message message);
    Task PublishUserOnlineAsync(Guid userId, User user);
    IAsyncEnumerable<Message> SubscribeToMessages(Guid conversationId, CancellationToken cancellationToken);
    IAsyncEnumerable<User> SubscribeToUserOnline(Guid userId, CancellationToken cancellationToken);
}

public class RedisPubSubService : IRedisPubSubService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPubSubService> _logger;

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
            var json = JsonSerializer.Serialize(message);
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
            var json = JsonSerializer.Serialize(user);
            await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
            _logger.LogInformation("Published user online status to channel {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish user online status to Redis");
        }
    }

    public IAsyncEnumerable<Message> SubscribeToMessages(
        Guid conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        return SubscribeToChannel<Message>($"chat:messages:{conversationId}", cancellationToken);
    }

    public IAsyncEnumerable<User> SubscribeToUserOnline(
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        return SubscribeToChannel<User>($"chat:online:{userId}", cancellationToken);
    }

    private async IAsyncEnumerable<T> SubscribeToChannel<T>(
        string channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelName = RedisChannel.Literal(channel);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var messageTask = new TaskCompletionSource<RedisValue>();
            using var _ = cancellationToken.Register(() => messageTask.TrySetCanceled());

            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync(channelName, (redisChannel, value) =>
            {
                messageTask.TrySetResult(value);
            });

            try
            {
                var completedTask = await Task.WhenAny(
                    messageTask.Task,
                    Task.Delay(Timeout.Infinite, cancellationToken));

                if (messageTask.Task.IsCompletedSuccessfully)
                {
                    var value = messageTask.Task.Result;
                    if (!value.IsNullOrEmpty)
                    {
                        var item = JsonSerializer.Deserialize<T>(value!);
                        if (item != null)
                        {
                            yield return item;
                        }
                    }
                }
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channelName);
            }
        }
    }
}