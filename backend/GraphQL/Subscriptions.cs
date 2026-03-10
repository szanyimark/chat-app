using System.Runtime.CompilerServices;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace ChatApp.Backend.GraphQL;

public class Subscription
{
    [Subscribe(MessageType = typeof(Message))]
    public IAsyncEnumerable<Message> MessageSent(
        [ID] string conversationId,
        [Service] IRedisPubSubService? redisPubSub,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(conversationId, out var parsedConversationId))
        {
            throw new GraphQLException("Invalid conversation ID format");
        }

        if (redisPubSub == null)
        {
            throw new GraphQLException("Redis pub/sub service is not available");
        }

        return redisPubSub.SubscribeToMessages(parsedConversationId, cancellationToken);
    }

    [Subscribe(MessageType = typeof(User))]
    public IAsyncEnumerable<User> UserOnline(
        [ID] string userId,
        [Service] IRedisPubSubService? redisPubSub,
        [Service] IPresenceService? presenceService,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new GraphQLException("Invalid user ID format");
        }

        // Note: Can't call async OnConnectedAsync here anymore since method is not async
        // This logic should be moved to connection initialization

        if (redisPubSub == null)
        {
            throw new GraphQLException("Redis pub/sub service is not available");
        }

        return redisPubSub.SubscribeToUserOnline(parsedUserId, cancellationToken);
    }

    [Subscribe(With = nameof(SubscribeToFriendRequestUpdated))]
    public FriendRequest FriendRequestUpdated(
        [ID] string userId,
        [EventMessage] FriendRequest friendRequest,
        [Service] ILogger<Subscription> logger)
    {
        logger.LogInformation("FriendRequestUpdated event delivered for userId argument {UserId}, request {RequestId}", userId, friendRequest.Id);

        return friendRequest;
    }

    public async ValueTask<ISourceStream<FriendRequest>> SubscribeToFriendRequestUpdated(
        [ID] string userId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ITopicEventReceiver topicEventReceiver,
        [Service] ILogger<Subscription> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("FriendRequestUpdated subscription request received. userId arg: {UserId}", userId);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            logger.LogWarning("FriendRequestUpdated rejected: invalid userId format {UserId}", userId);
            throw new GraphQLException("Invalid user ID format");
        }

        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId) || meId != parsedUserId)
        {
            logger.LogWarning("FriendRequestUpdated rejected: not allowed. meClaim={MeClaim}, parsedUserId={ParsedUserId}", meClaim, parsedUserId);
            throw new GraphQLException("Not allowed");
        }

        var topic = $"friendRequestUpdated_{parsedUserId}";
        logger.LogInformation("FriendRequestUpdated accepted for user {UserId}. Subscribing to topic {Topic}", parsedUserId, topic);

        return await topicEventReceiver.SubscribeAsync<FriendRequest>(topic, cancellationToken);
    }

    [Subscribe(With = nameof(SubscribeToFriendshipUpdated))]
    public bool FriendshipUpdated(
        [ID] string userId,
        [EventMessage] bool updated,
        [Service] ILogger<Subscription> logger)
    {
        logger.LogInformation("FriendshipUpdated event delivered for userId argument {UserId}", userId);
        return updated;
    }

    public async ValueTask<ISourceStream<bool>> SubscribeToFriendshipUpdated(
        [ID] string userId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ITopicEventReceiver topicEventReceiver,
        [Service] ILogger<Subscription> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("FriendshipUpdated subscription request received. userId arg: {UserId}", userId);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            logger.LogWarning("FriendshipUpdated rejected: invalid userId format {UserId}", userId);
            throw new GraphQLException("Invalid user ID format");
        }

        var meClaim = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(meClaim) || !Guid.TryParse(meClaim, out var meId) || meId != parsedUserId)
        {
            logger.LogWarning("FriendshipUpdated rejected: not allowed. meClaim={MeClaim}, parsedUserId={ParsedUserId}", meClaim, parsedUserId);
            throw new GraphQLException("Not allowed");
        }

        var topic = $"friendshipUpdated_{parsedUserId}";
        logger.LogInformation("FriendshipUpdated accepted for user {UserId}. Subscribing to topic {Topic}", parsedUserId, topic);

        return await topicEventReceiver.SubscribeAsync<bool>(topic, cancellationToken);
    }
}