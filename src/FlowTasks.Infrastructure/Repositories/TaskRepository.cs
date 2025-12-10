using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Infrastructure.Repositories;

public class TaskRepository : Repository<TaskProject>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TaskProject?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);
    }

    public async Task<TaskProject?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .Include(t => t.Sprint)
            .Include(t => t.Epic)
            .Include(t => t.Parent)
            .Include(t => t.Subtasks)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskProject>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskProject>> GetByAssigneeIdAsync(string assigneeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.AssigneeId == assigneeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskProject>> GetBySprintIdAsync(string sprintId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.SprintId == sprintId)
            .ToListAsync(cancellationToken);
    }
}

