using FlowTasks.Domain.Enums;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Domain.Entities;

public class TaskProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Key { get; set; } = string.Empty; // Ex: FLOW-123
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType Type { get; set; } = TaskType.Task;
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public required string ProjectId { get; set; }
    public string? AssigneeId { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Labels { get; set; } // JSON array or comma-separated
    public string? SprintId { get; set; }
    public string? EpicId { get; set; }
    public string? ParentId { get; set; }
    public string? Attachments { get; set; } // JSON array of URLs or base64
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User? Assignee { get; set; }
    public User Reporter { get; set; } = null!;
    public Sprint? Sprint { get; set; }
    public TaskProject? Epic { get; set; }
    public TaskProject? Parent { get; set; }
    public ICollection<TaskProject> Subtasks { get; set; } = [];
    public ICollection<TaskComment> Comments { get; set; } = [];
    public ICollection<TaskHistory> History { get; set; } = [];
}

