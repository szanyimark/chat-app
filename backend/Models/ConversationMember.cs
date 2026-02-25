using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models;

public enum MemberRole
{
    Member,
    Admin
}

public class ConversationMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}