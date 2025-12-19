using System.Text.Json;
using System.Text.RegularExpressions;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class TaskCommentService : ITaskCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectService _projectService;
    private readonly INotificationService _notificationService;

    public TaskCommentService(
        IUnitOfWork unitOfWork,
        IProjectService projectService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _projectService = projectService;
        _notificationService = notificationService;
    }

    public async Task<TaskCommentDto> CreateAsync(string taskId, string userId, CreateTaskCommentRequest request)
    {
        var task = await _unitOfWork.Tasks.Query()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (!await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        // Extract mentions (@username or @email)
        var mentions = ExtractMentions(request.Content);

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = request.Content,
            Mentions = mentions.Any() ? JsonSerializer.Serialize(mentions) : null,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TaskComments.AddAsync(comment);
        await _unitOfWork.CompleteAsync();

        await _notificationService.NotifyCommentAddedAsync(task.ProjectId, task.Key, comment.Id);

        return await MapToDtoAsync(comment);
    }

    public async Task<List<TaskCommentDto>> GetByTaskIdAsync(string taskId, string userId)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task == null || !await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var comments = await _unitOfWork.TaskComments.GetByTaskIdWithUserAsync(taskId);

        var result = new List<TaskCommentDto>();
        foreach (var comment in comments)
        {
            result.Add(await MapToDtoAsync(comment));
        }

        return result;
    }

    public async Task<TaskCommentDto> UpdateAsync(string commentId, string userId, CreateTaskCommentRequest request)
    {
        var comment = await _unitOfWork.TaskComments.Query()
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            throw new InvalidOperationException("Comment not found");
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own comments");
        }

        var mentions = ExtractMentions(request.Content);

        comment.Content = request.Content;
        comment.Mentions = mentions.Any() ? JsonSerializer.Serialize(mentions) : null;
        comment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.TaskComments.Update(comment);
        await _unitOfWork.CompleteAsync();

        return await MapToDtoAsync(comment);
    }

    public async Task DeleteAsync(string commentId, string userId)
    {
        var comment = await _unitOfWork.TaskComments.Query()
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            throw new InvalidOperationException("Comment not found");
        }

        if (comment.UserId != userId && !await _projectService.IsProjectAdminAsync(comment.Task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You cannot delete this comment");
        }

        _unitOfWork.TaskComments.Delete(comment);
        await _unitOfWork.CompleteAsync();
    }

    private List<string> ExtractMentions(string content)
    {
        var mentions = new List<string>();
        var pattern = @"@(\w+@[\w\.-]+\.\w+)"; // Match @email
        var matches = Regex.Matches(content, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                mentions.Add(match.Groups[1].Value);
            }
        }

        return mentions.Distinct().ToList();
    }

    private async Task<TaskCommentDto> MapToDtoAsync(TaskComment comment)
    {
        // Ensure User is loaded
        if (comment.User == null)
        {
            comment = await _unitOfWork.TaskComments.Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);
        }

        return new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            User = new UserDto
            {
                Id = comment.User.Id,
                Email = comment.User.Email ?? string.Empty,
                FirstName = comment.User.FirstName,
                LastName = comment.User.LastName,
                AvatarUrl = comment.User.AvatarUrl
            },
            Content = comment.Content,
            Mentions = comment.Mentions != null ? JsonSerializer.Deserialize<List<string>>(comment.Mentions) : null,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
