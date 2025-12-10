using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class TaskHistoryRepository : Repository<TaskHistory>, ITaskHistoryRepository
{
    public TaskHistoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TaskHistory>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(th => th.TaskId == taskId)
            .OrderByDescending(th => th.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskHistory>> GetByTaskIdWithUserAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(th => th.User)
            .Where(th => th.TaskId == taskId)
            .OrderByDescending(th => th.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

