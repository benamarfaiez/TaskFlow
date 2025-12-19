using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Interfaces;

public interface ISprintRepository : IRepository<Sprint>
{
    Task<IEnumerable<Sprint>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
    Task<Sprint?> GetActiveSprintByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
}

