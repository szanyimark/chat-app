using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models;

// Represents an accepted friendship edge between two users.
// Stored as two rows (A->B and B->A) for simple querying.
public class Friendship
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid FriendId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public User Friend { get; set; } = null!;
}
