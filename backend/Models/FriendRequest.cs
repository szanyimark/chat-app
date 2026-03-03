using System.ComponentModel.DataAnnotations;

namespace ChatApp.Backend.Models;

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

public class FriendRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FromUserId { get; set; }

    [Required]
    public Guid ToUserId { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
