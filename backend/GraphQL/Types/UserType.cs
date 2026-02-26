using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Backend.GraphQL.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Email).Type<NonNullType<StringType>>();
        descriptor.Field(u => u.Username).Type<NonNullType<StringType>>();
        descriptor.Field(u => u.Avatar).Type<StringType>();
        descriptor.Field(u => u.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(u => u.IsOnline).Type<NonNullType<BooleanType>>();
        descriptor.Field(u => u.LastSeenAt).Type<DateTimeType>();

        descriptor.Field("conversations")
            .Type<NonNullType<ListType<NonNullType<ConversationGraphType>>>>()
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                
                var memberIds = db.ConversationMembers
                    .Where(cm => cm.UserId == user.Id)
                    .Select(cm => cm.ConversationId)
                    .ToList();

                return db.Conversations
                    .Where(c => memberIds.Contains(c.Id))
                    .ToListAsync();
            });
    }
}