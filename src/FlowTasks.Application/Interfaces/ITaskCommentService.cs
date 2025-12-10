using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface ITaskCommentService
{
    Task<TaskCommentDto> CreateAsync(string taskId, string userId, CreateTaskCommentRequest request);
    Task<List<TaskCommentDto>> GetByTaskIdAsync(string taskId, string userId);
    Task<TaskCommentDto> UpdateAsync(string commentId, string userId, CreateTaskCommentRequest request);
    Task DeleteAsync(string commentId, string userId);
}

