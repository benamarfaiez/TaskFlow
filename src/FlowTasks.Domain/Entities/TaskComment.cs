namespace FlowTasks.Domain.Entities;

public class TaskComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string TaskId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Mentions { get; set; } // JSON array of user IDs
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public TaskProject Task { get; set; } = null!;
    public User User { get; set; } = null!;
}

