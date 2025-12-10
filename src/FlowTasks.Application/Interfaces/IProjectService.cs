using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(string userId, CreateProjectRequest request);
    Task<ProjectDto?> GetByIdAsync(string id, string userId);
    Task<List<ProjectDto>> GetUserProjectsAsync(string userId);
    Task<ProjectDto> UpdateAsync(string id, string userId, UpdateProjectRequest request);
    Task DeleteAsync(string id, string userId);
    Task<bool> IsProjectMemberAsync(string projectId, string userId);
    Task<bool> IsProjectAdminAsync(string projectId, string userId);
}

