using System.Text.Json;
using System.Text.RegularExpressions;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class TaskCommentService : ITaskCommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;
    private readonly INotificationService _notificationService;

    public TaskCommentService(
        ApplicationDbContext context,
        IProjectService projectService,
        INotificationService notificationService)
    {
        _context = context;
        _projectService = projectService;
        _notificationService = notificationService;
    }

    public async Task<TaskCommentDto> CreateAsync(string taskId, string userId, CreateTaskCommentRequest request)
    {
        var task = await _context.Tasks
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

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyCommentAddedAsync(task.ProjectId, task.Key, comment.Id);

        return await MapToDtoAsync(comment);
    }

    public async Task<List<TaskCommentDto>> GetByTaskIdAsync(string taskId, string userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null || !await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var comments = await _context.TaskComments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<TaskCommentDto>();
        foreach (var comment in comments)
        {
            result.Add(await MapToDtoAsync(comment));
        }

        return result;
    }

    public async Task<TaskCommentDto> UpdateAsync(string commentId, string userId, CreateTaskCommentRequest request)
    {
        var comment = await _context.TaskComments
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

        await _context.SaveChangesAsync();

        return await MapToDtoAsync(comment);
    }

    public async Task DeleteAsync(string commentId, string userId)
    {
        var comment = await _context.TaskComments
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

        _context.TaskComments.Remove(comment);
        await _context.SaveChangesAsync();
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
        await _context.Entry(comment).Reference(c => c.User).LoadAsync();

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

