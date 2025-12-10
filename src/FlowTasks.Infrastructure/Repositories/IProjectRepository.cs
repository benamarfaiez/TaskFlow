using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default);
}

