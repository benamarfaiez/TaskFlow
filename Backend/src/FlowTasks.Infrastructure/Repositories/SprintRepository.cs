using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class SprintRepository : Repository<Sprint>, ISprintRepository
{
    public SprintRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Sprint>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Sprint?> GetActiveSprintByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.ProjectId == projectId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

}

