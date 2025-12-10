using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Repositories;

public interface ITaskRepository : IRepository<TaskProject>
{
    Task<TaskProject?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<TaskProject?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskProject>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskProject>> GetByAssigneeIdAsync(string assigneeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskProject>> GetBySprintIdAsync(string sprintId, CancellationToken cancellationToken = default);
}

