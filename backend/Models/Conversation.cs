using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models;

public enum ConversationType
{
    Private,
    Group
}

public class Conversation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public ConversationType Type { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}