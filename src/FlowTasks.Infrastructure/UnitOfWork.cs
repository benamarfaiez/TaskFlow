using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Interfaces;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlowTasks.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProjectRepository? _projects;
    private ITaskRepository? _tasks;
    private ITaskCommentRepository? _taskComments;
    private ISprintRepository? _sprints;
    private IProjectMemberRepository? _projectMembers;
    private ITaskHistoryRepository? _taskHistories;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects
    {
        get
        {
            _projects ??= new ProjectRepository(_context);
            return _projects;
        }
    }

    public ITaskRepository Tasks
    {
        get
        {
            _tasks ??= new TaskRepository(_context);
            return _tasks;
        }
    }

    public ITaskCommentRepository TaskComments
    {
        get
        {
            _taskComments ??= new TaskCommentRepository(_context);
            return _taskComments;
        }
    }

    public ISprintRepository Sprints
    {
        get
        {
            _sprints ??= new SprintRepository(_context);
            return _sprints;
        }
    }

    public IProjectMemberRepository ProjectMembers
    {
        get
        {
            _projectMembers ??= new ProjectMemberRepository(_context);
            return _projectMembers;
        }
    }

    public ITaskHistoryRepository TaskHistories
    {
        get
        {
            _taskHistories ??= new TaskHistoryRepository(_context);
            return _taskHistories;
        }
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

