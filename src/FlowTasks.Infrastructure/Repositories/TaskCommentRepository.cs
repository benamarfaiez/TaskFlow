using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class TaskCommentRepository : Repository<TaskComment>, ITaskCommentRepository
{
    public TaskCommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TaskComment>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tc => tc.TaskId == taskId)
            .OrderByDescending(tc => tc.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskComment>> GetByTaskIdWithUserAsync(string taskId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(tc => tc.User)
            .Where(tc => tc.TaskId == taskId)
            .OrderByDescending(tc => tc.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

