using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface ITaskHistoryService
{
    Task<List<TaskHistoryDto>> GetByTaskIdAsync(string taskId, string userId);
}

