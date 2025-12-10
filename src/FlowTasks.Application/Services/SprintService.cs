using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class SprintService : ISprintService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;

    public SprintService(ApplicationDbContext context, IProjectService projectService)
    {
        _context = context;
        _projectService = projectService;
    }

    public async Task<SprintDto> CreateAsync(string projectId, string userId, CreateSprintRequest request)
    {
        if (!await _projectService.IsProjectAdminAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can create sprints");
        }

        // Deactivate other active sprints in the project
        var activeSprints = await _context.Sprints
            .Where(s => s.ProjectId == projectId && s.IsActive)
            .ToListAsync();

        foreach (var s in activeSprints)
        {
            s.IsActive = false;
        }

        var sprint = new Sprint
        {
            ProjectId = projectId,
            Name = request.Name,
            Goal = request.Goal,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sprints.Add(sprint);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(sprint.Id, userId) ?? throw new InvalidOperationException("Failed to create sprint");
    }

    public async Task<SprintDto?> GetByIdAsync(string id, string userId)
    {
        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sprint == null || !await _projectService.IsProjectMemberAsync(sprint.ProjectId, userId))
        {
            return null;
        }

        var taskCount = await _context.Tasks.CountAsync(t => t.SprintId == id);

        return new SprintDto
        {
            Id = sprint.Id,
            ProjectId = sprint.ProjectId,
            Name = sprint.Name,
            Goal = sprint.Goal,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            IsActive = sprint.IsActive,
            CreatedAt = sprint.CreatedAt,
            TaskCount = taskCount
        };
    }

    public async Task<List<SprintDto>> GetByProjectIdAsync(string projectId, string userId)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var sprints = await _context.Sprints
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var result = new List<SprintDto>();
        foreach (var sprint in sprints)
        {
            var taskCount = await _context.Tasks.CountAsync(t => t.SprintId == sprint.Id);
            result.Add(new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                IsActive = sprint.IsActive,
                CreatedAt = sprint.CreatedAt,
                TaskCount = taskCount
            });
        }

        return result;
    }

    public async Task<SprintDto> UpdateAsync(string id, string userId, CreateSprintRequest request)
    {
        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sprint == null)
        {
            throw new InvalidOperationException("Sprint not found");
        }

        if (!await _projectService.IsProjectAdminAsync(sprint.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can update sprints");
        }

        sprint.Name = request.Name;
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id, userId) ?? throw new InvalidOperationException("Failed to update sprint");
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sprint == null)
        {
            throw new InvalidOperationException("Sprint not found");
        }

        if (!await _projectService.IsProjectAdminAsync(sprint.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can delete sprints");
        }

        _context.Sprints.Remove(sprint);
        await _context.SaveChangesAsync();
    }
}

