using ChatApp.Backend.Models;
using HotChocolate.Types;

namespace ChatApp.Backend.GraphQL.Types;

public class ConversationMemberType : ObjectType<ConversationMember>
{
    protected override void Configure(IObjectTypeDescriptor<ConversationMember> descriptor)
    {
        descriptor.Name("ConversationMember");

        descriptor.Field(cm => cm.Id).Type<NonNullType<IdType>>();
        descriptor.Field(cm => cm.Role).Type<NonNullType<MemberRoleEnum>>();
        descriptor.Field(cm => cm.JoinedAt).Type<NonNullType<DateTimeType>>();

        descriptor.Field("user")
            .Type<NonNullType<UserType>>()
            .Resolve(ctx => ctx.Parent<ConversationMember>().User);

        descriptor.Field("conversation")
            .Type<NonNullType<ConversationGraphType>>()
            .Resolve(ctx => ctx.Parent<ConversationMember>().Conversation);
    }
}

public class MemberRoleEnum : EnumType<MemberRole>
{
    protected override void Configure(IEnumTypeDescriptor<MemberRole> descriptor)
    {
        descriptor.Name("MemberRole");
    }
}