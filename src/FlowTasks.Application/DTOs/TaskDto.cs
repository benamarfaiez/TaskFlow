using FlowTasks.Domain.Enums;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Application.DTOs;

public class TaskDto
{
    public string Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType Type { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string ProjectId { get; set; }
    public string? AssigneeId { get; set; }
    public UserDto? Assignee { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public UserDto Reporter { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public List<string>? Labels { get; set; }
    public string? SprintId { get; set; }
    public string? EpicId { get; set; }
    public string? ParentId { get; set; }
    public List<string>? Attachments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

