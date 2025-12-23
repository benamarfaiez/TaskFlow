namespace FlowTasks.Application.DTOs;

public class TaskCommentDto
{
    public required string Id { get; set; }
    public required string TaskId { get; set; }
    public UserDto User { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public List<string>? Mentions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

