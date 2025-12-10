using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Repositories;

public interface ITaskCommentRepository : IRepository<TaskComment>
{
    Task<IEnumerable<TaskComment>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskComment>> GetByTaskIdWithUserAsync(string taskId, CancellationToken cancellationToken = default);
}

