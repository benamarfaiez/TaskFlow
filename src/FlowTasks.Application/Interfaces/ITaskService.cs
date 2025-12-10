using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(string projectId, string userId, CreateTaskRequest request);
    Task<TaskDto?> GetByIdAsync(string id, string userId);
    Task<PagedResult<TaskDto>> GetFilteredAsync(string projectId, string userId, TaskFilterRequest filter);
    Task<TaskDto> UpdateAsync(string id, string userId, UpdateTaskRequest request);
    Task DeleteAsync(string id, string userId);
    Task<BoardDto> GetBoardAsync(string projectId, string userId);
}

