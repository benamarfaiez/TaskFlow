using FlowTasks.Domain.Entities;

namespace FlowTasks.Infrastructure.Interfaces;

public interface IProjectMemberRepository : IRepository<ProjectMember>
{
    Task<ProjectMember?> GetByProjectAndUserAsync(string projectId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectMember>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectMember>> GetByProjectIdWithUserAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(string projectId, string userId, CancellationToken cancellationToken = default);
}

