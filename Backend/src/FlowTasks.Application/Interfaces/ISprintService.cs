using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface ISprintService
{
    Task<SprintDto> CreateAsync(string projectId, string userId, CreateSprintRequest request);
    Task<SprintDto?> GetByIdAsync(string id, string userId);
    Task<List<SprintDto>> GetByProjectIdAsync(string projectId, string userId);
    Task<SprintDto> UpdateAsync(string id, string userId, CreateSprintRequest request);
    Task DeleteAsync(string id, string userId);
}

