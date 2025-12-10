namespace FlowTasks.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    ITaskRepository Tasks { get; }
    ITaskCommentRepository TaskComments { get; }
    ISprintRepository Sprints { get; }
    IProjectMemberRepository ProjectMembers { get; }
    ITaskHistoryRepository TaskHistories { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

