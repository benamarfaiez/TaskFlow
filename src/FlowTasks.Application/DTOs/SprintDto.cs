namespace FlowTasks.Application.DTOs;

public class SprintDto
{
    public string Id { get; set; }
    public string ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TaskCount { get; set; }
}

