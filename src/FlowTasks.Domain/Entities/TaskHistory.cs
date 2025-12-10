namespace FlowTasks.Domain.Entities;

public class TaskHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TaskId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty; // Status, Assignee, Priority, etc.
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TaskProject Task { get; set; } = null!;
    public User User { get; set; } = null!;
}

