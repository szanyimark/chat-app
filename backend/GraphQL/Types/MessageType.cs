using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Backend.GraphQL.Types;

public class MessageType : ObjectType<Message>
{
    protected override void Configure(IObjectTypeDescriptor<Message> descriptor)
    {
        descriptor.Name("Message");

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Content).Type<NonNullType<StringType>>();
        descriptor.Field(m => m.CreatedAt).Type<NonNullType<DateTimeType>>();

        descriptor.Field("conversation")
            .Type<NonNullType<ConversationGraphType>>()
            .Resolve(ctx =>
            {
                var message = ctx.Parent<Message>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                
                return db.Conversations.FindAsync(message.ConversationId);
            });

        descriptor.Field("sender")
            .Type<NonNullType<UserType>>()
            .Resolve(ctx =>
            {
                var message = ctx.Parent<Message>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                
                return db.Users.FindAsync(message.SenderId);
            });
    }
}