using FlowTasks.Domain.Enums;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Application.DTOs;

public class TaskFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskType? Type { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? AssigneeId { get; set; }
    public string? SprintId { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

