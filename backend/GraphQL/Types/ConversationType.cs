using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Backend.GraphQL.Types;

public class ConversationGraphType : ObjectType<Conversation>
{
    protected override void Configure(IObjectTypeDescriptor<Conversation> descriptor)
    {
        descriptor.Name("Conversation");

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Type).Type<NonNullType<ConversationTypeEnum>>();
        descriptor.Field(c => c.Name).Type<StringType>();
        descriptor.Field(c => c.CreatedAt).Type<NonNullType<DateTimeType>>();

        descriptor.Field("members")
            .Type<NonNullType<ListType<NonNullType<UserType>>>>()
            .Resolve(ctx =>
            {
                var conversation = ctx.Parent<Conversation>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                
                return db.ConversationMembers
                    .Where(cm => cm.ConversationId == conversation.Id)
                    .Include(cm => cm.User)
                    .Select(cm => cm.User)
                    .ToListAsync();
            });

        descriptor.Field("messages")
            .Type<NonNullType<ListType<NonNullType<MessageType>>>>()
            .Argument("limit", a => a.Type<IntType>().DefaultValue(50))
            .Resolve(ctx =>
            {
                var conversation = ctx.Parent<Conversation>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                var limit = ctx.ArgumentValue<int>("limit");
                
                return db.Messages
                    .Where(m => m.ConversationId == conversation.Id)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            });
    }
}

public class ConversationTypeEnum : EnumType<ConversationType>
{
    protected override void Configure(IEnumTypeDescriptor<ConversationType> descriptor)
    {
        descriptor.Name("ConversationType");
    }
}