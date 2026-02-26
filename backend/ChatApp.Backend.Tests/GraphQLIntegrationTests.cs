using System.Security.Claims;
using ChatApp.Backend.Configuration;
using ChatApp.Backend.Data;
using ChatApp.Backend.GraphQL;
using ChatApp.Backend.GraphQL.Types;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ChatApp.Backend.Tests;

public class GraphQLIntegrationTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private IJwtService CreateJwtService()
    {
        var config = new AppConfig
        {
            JwtKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
            JwtIssuer = "ChatApp",
            JwtAudience = "ChatApp",
            JwtExpiryMinutes = 60
        };
        return new JwtService(config);
    }

    private Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        
        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new Claim("userId", userId.Value.ToString()),
                new Claim(ClaimTypes.Email, "test@test.com"),
                new Claim("username", "testuser")
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        
        mock.Setup(x => x.HttpContext).Returns(context);
        return mock;
    }

    [Fact]
    public async Task Register_ShouldCreateNewUser()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        var input = new RegisterInput("newuser@test.com", "newuser", "password123");
        
        // Act
        var result = await mutation.Register(input, db, jwtService);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result.User.Username);
        Assert.Equal("newuser@test.com", result.User.Email);
        Assert.NotNull(result.Token);
        
        // Verify user was saved to database
        var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        // Create existing user
        var existingUser = new User
        {
            Email = "existing@test.com",
            Username = "existing",
            Password = "hash"
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();
        
        var input = new RegisterInput("existing@test.com", "newuser", "password123");
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.Register(input, db, jwtService));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        // Create existing user
        var existingUser = new User
        {
            Email = "existing@test.com",
            Username = "existing",
            Password = "hash"
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();
        
        var input = new RegisterInput("newuser@test.com", "existing", "password123");
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.Register(input, db, jwtService));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        // Create user with hashed password
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("password123"));
        var passwordHash = Convert.ToBase64String(bytes);
        
        var user = new User
        {
            Email = "login@test.com",
            Username = "loginuser",
            Password = passwordHash
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var input = new LoginInput("login@test.com", "password123");
        
        // Act
        var result = await mutation.Login(input, db, jwtService, null);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.User.Id);
        Assert.NotNull(result.Token);
        Assert.True(result.User.IsOnline);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        var input = new LoginInput("nonexistent@test.com", "password123");
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.Login(input, db, jwtService, null));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var jwtService = CreateJwtService();
        var mutation = new Mutation();
        
        // Create user with hashed password
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("correctpassword"));
        var passwordHash = Convert.ToBase64String(bytes);
        
        var user = new User
        {
            Email = "login@test.com",
            Username = "loginuser",
            Password = passwordHash
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var input = new LoginInput("login@test.com", "wrongpassword");
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.Login(input, db, jwtService, null));
    }

    [Fact]
    public async Task GetMe_WithAuthenticatedUser_ShouldReturnUser()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "me@test.com",
            Username = "meuser",
            Password = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var query = new Query();
        
        // Act
        var result = await query.GetMe(httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("me@test.com", result.Email);
    }

    [Fact]
    public async Task GetMe_WithoutAuthentication_ShouldReturnNull()
    {
        // Arrange
        var db = CreateDbContext();
        var httpContextAccessor = CreateMockHttpContextAccessor(null);
        var query = new Query();
        
        // Act
        var result = await query.GetMe(httpContextAccessor.Object, db);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var db = CreateDbContext();
        
        // Add multiple users
        db.Users.Add(new User { Email = "user1@test.com", Username = "user1", Password = "hash" });
        db.Users.Add(new User { Email = "user2@test.com", Username = "user2", Password = "hash" });
        db.Users.Add(new User { Email = "user3@test.com", Username = "user3", Password = "hash" });
        await db.SaveChangesAsync();
        
        var query = new Query();
        
        // Act
        var result = await query.GetUsers(db);
        
        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "findme@test.com",
            Username = "findme",
            Password = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var query = new Query();
        
        // Act
        var result = await query.GetUserById(userId, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var db = CreateDbContext();
        var query = new Query();
        
        // Act
        var result = await query.GetUserById(Guid.NewGuid(), db);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateConversation_ShouldCreateConversation()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        var input = new CreateConversationInput(ConversationType.Private, null);
        
        // Act
        var result = await mutation.CreateConversation(input, httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConversationType.Private, result.Type);
        
        // Verify member was added
        var member = await db.ConversationMembers.FirstOrDefaultAsync(cm => cm.UserId == userId);
        Assert.NotNull(member);
        Assert.Equal(MemberRole.Admin, member.Role);
    }

    [Fact]
    public async Task CreateConversation_WithGroupType_ShouldCreateGroupConversation()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        var input = new CreateConversationInput(ConversationType.Group, "Test Group");
        
        // Act
        var result = await mutation.CreateConversation(input, httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConversationType.Group, result.Type);
        Assert.Equal("Test Group", result.Name);
        
        // Verify member was added with Admin role
        var member = await db.ConversationMembers.FirstOrDefaultAsync(cm => cm.UserId == userId);
        Assert.NotNull(member);
        Assert.Equal(MemberRole.Admin, member.Role);
    }

    [Fact]
    public async Task CreateConversation_NotAuthenticated_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(null); // No user
        var mutation = new Mutation();
        
        var input = new CreateConversationInput(ConversationType.Private, null);
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() =>
            mutation.CreateConversation(input, httpContextAccessor.Object, db));
    }

    [Fact]
    public async Task GetMyConversations_ShouldReturnUserConversations()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        
        // Create conversations and memberships
        var conv1 = new Conversation { Id = Guid.NewGuid(), Type = ConversationType.Private };
        var conv2 = new Conversation { Id = Guid.NewGuid(), Type = ConversationType.Group, Name = "Test Group" };
        
        db.Users.Add(user);
        db.Conversations.Add(conv1);
        db.Conversations.Add(conv2);
        
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv1.Id, UserId = userId, Role = MemberRole.Member });
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv2.Id, UserId = userId, Role = MemberRole.Admin });
        
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var query = new Query();
        
        // Act
        var result = await query.GetMyConversations(httpContextAccessor.Object, db);
        
        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMyConversations_ShouldOnlyReturnUserConversations()
    {
        // Arrange
        var db = CreateDbContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        // Create two users
        var user1 = new User { Id = userId1, Email = "user1@test.com", Username = "user1", Password = "hash" };
        var user2 = new User { Id = userId2, Email = "user2@test.com", Username = "user2", Password = "hash" };
        
        // Create conversations
        var conv1 = new Conversation { Id = Guid.NewGuid(), Type = ConversationType.Private }; // User1's conversation
        var conv2 = new Conversation { Id = Guid.NewGuid(), Type = ConversationType.Group, Name = "User2 Group" }; // User2's conversation
        var conv3 = new Conversation { Id = Guid.NewGuid(), Type = ConversationType.Group, Name = "Shared" }; // Both users' conversation
        
        db.Users.Add(user1);
        db.Users.Add(user2);
        db.Conversations.Add(conv1);
        db.Conversations.Add(conv2);
        db.Conversations.Add(conv3);
        
        // Add memberships
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv1.Id, UserId = userId1, Role = MemberRole.Admin });
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv2.Id, UserId = userId2, Role = MemberRole.Admin });
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv3.Id, UserId = userId1, Role = MemberRole.Member });
        db.ConversationMembers.Add(new ConversationMember { ConversationId = conv3.Id, UserId = userId2, Role = MemberRole.Member });
        
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId1);
        var query = new Query();
        
        // Act
        var result = await query.GetMyConversations(httpContextAccessor.Object, db);
        
        // Assert - User1 should only see conv1 and conv3, not conv2
        Assert.Equal(2, result.Count);
        var conversationIds = result.Select(c => c.Id).ToList();
        Assert.Contains(conv1.Id, conversationIds);
        Assert.Contains(conv3.Id, conversationIds);
        Assert.DoesNotContain(conv2.Id, conversationIds);
    }

    [Fact]
    public async Task GetMyConversations_NotAuthenticated_ShouldReturnEmptyList()
    {
        // Arrange
        var db = CreateDbContext();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(null); // No user
        var query = new Query();
        
        // Act
        var result = await query.GetMyConversations(httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnConversation_WhenUserIsMember()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User { Id = userId, Email = "user@test.com", Username = "user", Password = "hash" };
        
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Private,
            Name = "My Conversation"
        };
        
        db.Users.Add(user);
        db.Conversations.Add(conversation);
        db.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = MemberRole.Admin
        });
        
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var query = new Query();
        
        // Act
        var result = await query.GetConversationById(conversationId, httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(conversationId, result.Id);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnNull_WhenUserIsNotMember()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var user = new User { Id = userId, Email = "user@test.com", Username = "user", Password = "hash" };
        var otherUser = new User { Id = otherUserId, Email = "other@test.com", Username = "other", Password = "hash" };
        
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Private,
            Name = "Private Conversation"
        };
        
        db.Users.Add(user);
        db.Users.Add(otherUser);
        db.Conversations.Add(conversation);
        db.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversationId,
            UserId = otherUserId,
            Role = MemberRole.Admin
        });
        
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var query = new Query();
        
        // Act
        var result = await query.GetConversationById(conversationId, httpContextAccessor.Object, db);
        
        // Assert - Should return null because user is not a member
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConversationById_ShouldReturnNull_WhenNotAuthenticated()
    {
        // Arrange
        var db = CreateDbContext();
        
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Private,
            Name = "Private Conversation"
        };
        
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(null);
        var query = new Query();
        
        // Act
        var result = await query.GetConversationById(conversationId, httpContextAccessor.Object, db);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Heartbeat_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        
        var mockPresenceService = new Mock<IPresenceService>();
        mockPresenceService.Setup(x => x.ReceiveHeartbeatAsync(userId)).Returns(Task.CompletedTask);
        
        var mutation = new Mutation();
        
        // Act
        var result = await mutation.Heartbeat(httpContextAccessor.Object, mockPresenceService.Object);
        
        // Assert
        Assert.True(result);
        mockPresenceService.Verify(x => x.ReceiveHeartbeatAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Heartbeat_WithoutAuthentication_ShouldReturnFalse()
    {
        // Arrange
        var httpContextAccessor = CreateMockHttpContextAccessor(null);
        var mockPresenceService = new Mock<IPresenceService>();
        
        var mutation = new Mutation();
        
        // Act
        var result = await mutation.Heartbeat(httpContextAccessor.Object, mockPresenceService.Object);
        
        // Assert
        Assert.False(result);
        mockPresenceService.Verify(x => x.ReceiveHeartbeatAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task JoinConversation_ShouldAddMember()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Group,
            Name = "Test Group"
        };
        
        db.Users.Add(user);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        // Act
        var result = await mutation.JoinConversation(conversationId, httpContextAccessor.Object, db);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(conversationId, result.Id);
        
        // Verify member was added
        var member = await db.ConversationMembers.FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
        Assert.NotNull(member);
        Assert.Equal(MemberRole.Member, member.Role);
    }

    [Fact]
    public async Task JoinConversation_AlreadyMember_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Group,
            Name = "Test Group"
        };
        
        db.Users.Add(user);
        db.Conversations.Add(conversation);
        db.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = MemberRole.Member
        });
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.JoinConversation(conversationId, httpContextAccessor.Object, db));
    }

    [Fact]
    public async Task JoinConversation_NotFound_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.JoinConversation(Guid.NewGuid(), httpContextAccessor.Object, db));
    }

    [Fact]
    public async Task SendMessage_ShouldCreateMessage()
    {
        // Arrange
        var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Username = "user",
            Password = "hash"
        };
        
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Group,
            Name = "Test Group"
        };
        
        db.Users.Add(user);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(userId);
        var mutation = new Mutation();
        
        var input = new SendMessageInput(conversationId, "Hello, world!");
        
        // Act
        var result = await mutation.SendMessage(input, httpContextAccessor.Object, db, null);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, world!", result.Content);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal(userId, result.SenderId);
        
        // Verify message was saved
        var savedMessage = await db.Messages.FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.SenderId == userId);
        Assert.NotNull(savedMessage);
        Assert.Equal("Hello, world!", savedMessage.Content);
    }

    [Fact]
    public async Task SendMessage_WithoutAuthentication_ShouldThrowException()
    {
        // Arrange
        var db = CreateDbContext();
        var conversationId = Guid.NewGuid();
        
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Group,
            Name = "Test Group"
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        
        var httpContextAccessor = CreateMockHttpContextAccessor(null);
        var mutation = new Mutation();
        
        var input = new SendMessageInput(conversationId, "Hello!");
        
        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() => mutation.SendMessage(input, httpContextAccessor.Object, db, null));
    }
}