namespace FlowTasks.Domain.Entities;

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Key { get; set; } = string.Empty; // Ex: FLOW
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public required string OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<TaskProject> Tasks { get; set; } = [];
    public ICollection<Sprint> Sprints { get; set; } = [];
}

