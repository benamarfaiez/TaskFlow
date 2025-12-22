using FlowTasks.Domain.Enums;

namespace FlowTasks.Application.DTOs;

public class ProjectMemberDto
{
    public required string Id { get; set; }
    public required string ProjectId { get; set; }
    public UserDto User { get; set; } = null!;
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

