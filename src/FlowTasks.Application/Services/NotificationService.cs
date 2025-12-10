using FlowTasks.Application.Interfaces;
using FlowTasks.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowTasks.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<TaskHub> _hubContext;

    public NotificationService(IHubContext<TaskHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTaskCreatedAsync(string projectId, string taskKey)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("TaskCreated", taskKey);
    }

    public async Task NotifyTaskUpdatedAsync(string projectId, string taskKey)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("TaskUpdated", taskKey);
    }

    public async Task NotifyTaskMovedAsync(string projectId, string taskKey, string status)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("TaskMoved", taskKey, status);
    }

    public async Task NotifyTaskDeletedAsync(string projectId, string taskKey)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("TaskDeleted", taskKey);
    }

    public async Task NotifyCommentAddedAsync(string projectId, string taskKey, string commentId)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("CommentAdded", taskKey, commentId);
    }

    public async Task NotifyUserAssignedAsync(string projectId, string taskKey, string? userId)
    {
        await _hubContext.Clients.Group($"project-{projectId}").SendAsync("UserAssigned", taskKey, userId);
    }
}

