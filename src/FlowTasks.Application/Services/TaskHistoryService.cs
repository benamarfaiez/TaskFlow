using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class TaskHistoryService : ITaskHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;

    public TaskHistoryService(ApplicationDbContext context, IProjectService projectService)
    {
        _context = context;
        _projectService = projectService;
    }

    public async Task<List<TaskHistoryDto>> GetByTaskIdAsync(string taskId, string userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null || !await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var histories = await _context.TaskHistories
            .Include(h => h.User)
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return histories.Select(h => new TaskHistoryDto
        {
            Id = h.Id,
            TaskId = h.TaskId,
            User = new UserDto
            {
                Id = h.User.Id,
                Email = h.User.Email ?? string.Empty,
                FirstName = h.User.FirstName,
                LastName = h.User.LastName,
                AvatarUrl = h.User.AvatarUrl
            },
            Field = h.Field,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            CreatedAt = h.CreatedAt
        }).ToList();
    }
}

