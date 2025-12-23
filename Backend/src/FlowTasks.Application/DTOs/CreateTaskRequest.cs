using FlowTasks.Domain.Enums;

namespace FlowTasks.Application.DTOs;

public class CreateTaskRequest
{
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType Type { get; set; } = TaskType.Task;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string>? Labels { get; set; }
    public string? SprintId { get; set; }
    public string? EpicId { get; set; }
    public string? ParentId { get; set; }
    public List<string>? Attachments { get; set; }
}

