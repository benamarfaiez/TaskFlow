namespace FlowTasks.Application.DTOs;

public class TaskHistoryDto
{
    public string Id { get; set; }
    public string TaskId { get; set; }
    public UserDto User { get; set; } = null!;
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

