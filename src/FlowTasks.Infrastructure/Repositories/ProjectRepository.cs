using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
    }

    public async Task<Project?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .Include(p => p.Tasks)
            .Include(p => p.Sprints)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}

