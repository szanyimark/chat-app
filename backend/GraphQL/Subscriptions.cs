using System.Runtime.CompilerServices;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using HotChocolate;
using HotChocolate.Types;

namespace ChatApp.Backend.GraphQL;

public class Subscription
{
    [Subscribe]
    public async IAsyncEnumerable<Message> MessageSent(
        Guid conversationId,
        [Service] IRedisPubSubService? redisPubSub,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (redisPubSub == null)
        {
            // Redis not available - subscriptions won't work
            // In production, Redis should always be available
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }

        await foreach (var message in redisPubSub.SubscribeToMessages(conversationId, cancellationToken))
        {
            yield return message;
        }
    }

    [Subscribe]
    public async IAsyncEnumerable<User> UserOnline(
        Guid userId,
        [Service] IRedisPubSubService? redisPubSub,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (redisPubSub == null)
        {
            // Redis not available - user online tracking won't work
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }

        await foreach (var user in redisPubSub.SubscribeToUserOnline(userId, cancellationToken))
        {
            yield return user;
        }
    }
}