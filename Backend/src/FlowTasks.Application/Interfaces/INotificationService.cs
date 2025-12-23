namespace FlowTasks.Application.Interfaces;

public interface INotificationService
{
    Task NotifyTaskCreatedAsync(string projectId, string taskKey);
    Task NotifyTaskUpdatedAsync(string projectId, string taskKey);
    Task NotifyTaskMovedAsync(string projectId, string taskKey, string status);
    Task NotifyTaskDeletedAsync(string projectId, string taskKey);
    Task NotifyCommentAddedAsync(string projectId, string taskKey, string commentId);
    Task NotifyUserAssignedAsync(string projectId, string taskKey, string? userId);
}

