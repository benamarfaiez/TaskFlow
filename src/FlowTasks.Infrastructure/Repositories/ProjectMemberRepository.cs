using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class ProjectMemberRepository : Repository<ProjectMember>, IProjectMemberRepository
{
    public ProjectMemberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ProjectMember?> GetByProjectAndUserAsync(string projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<ProjectMember>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProjectMember>> GetByProjectIdWithUserAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProjectMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pm => pm.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsMemberAsync(string projectId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);
    }
}

