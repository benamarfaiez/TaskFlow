using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface IProjectMemberService
{
    Task<ProjectMemberDto> AddMemberAsync(string projectId, string userId, AddProjectMemberRequest request);
    Task<List<ProjectMemberDto>> GetMembersAsync(string projectId, string userId);
    Task RemoveMemberAsync(string projectId, string memberId, string userId);
}

