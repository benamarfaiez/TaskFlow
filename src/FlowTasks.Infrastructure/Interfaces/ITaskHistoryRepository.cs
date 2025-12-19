using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Interfaces;

public interface ITaskHistoryRepository : IRepository<TaskHistory>
{
    Task<IEnumerable<TaskHistory>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskHistory>> GetByTaskIdWithUserAsync(string taskId, CancellationToken cancellationToken = default);
}

