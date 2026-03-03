using ChatApp.Backend.Data;
using ChatApp.Backend.Models;
using HotChocolate.Types;

namespace ChatApp.Backend.GraphQL.Types;

public class FriendRequestType : ObjectType<FriendRequest>
{
    protected override void Configure(IObjectTypeDescriptor<FriendRequest> descriptor)
    {
        descriptor.Name("FriendRequest");

        descriptor.Field(fr => fr.Id).Type<NonNullType<IdType>>();
        descriptor.Field(fr => fr.Status).Type<NonNullType<EnumType<FriendRequestStatus>>>();
        descriptor.Field(fr => fr.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(fr => fr.UpdatedAt).Type<NonNullType<DateTimeType>>();

        descriptor.Field("fromUser")
            .Type<NonNullType<UserType>>()
            .Resolve(async ctx =>
            {
                var fr = ctx.Parent<FriendRequest>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                return await db.Users.FindAsync(fr.FromUserId) ?? throw new GraphQLException("User not found");
            });

        descriptor.Field("toUser")
            .Type<NonNullType<UserType>>()
            .Resolve(async ctx =>
            {
                var fr = ctx.Parent<FriendRequest>();
                var db = ctx.Services.GetRequiredService<AppDbContext>();
                return await db.Users.FindAsync(fr.ToUserId) ?? throw new GraphQLException("User not found");
            });
    }
}
