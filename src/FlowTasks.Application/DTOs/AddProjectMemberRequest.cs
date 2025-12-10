using FlowTasks.Domain.Enums;

namespace FlowTasks.Application.DTOs;

public class AddProjectMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public ProjectRole Role { get; set; } = ProjectRole.Member;
}

