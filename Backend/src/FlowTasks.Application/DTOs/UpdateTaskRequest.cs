using FlowTasks.Domain.Enums;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Application.DTOs;

public class UpdateTaskRequest
{
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public TaskType? Type { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string>? Labels { get; set; }
    public string? SprintId { get; set; }
    public string? EpicId { get; set; }
    public string? ParentId { get; set; }
    public List<string>? Attachments { get; set; }
}

