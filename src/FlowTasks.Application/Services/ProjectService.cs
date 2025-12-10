using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDto> CreateAsync(string userId, CreateProjectRequest request)
    {
        var project = new Project
        {
            Key = request.Key.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        
        // Add owner as admin member
        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRole.Admin
        });

        await _context.SaveChangesAsync();

        return await GetByIdAsync(project.Id, userId) ?? throw new InvalidOperationException("Failed to create project");
    }

    public async Task<ProjectDto?> GetByIdAsync(string id, string userId)
    {
        if (!await IsProjectMemberAsync(id, userId))
        {
            return null;
        }

        var project = await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return null;

        var memberCount = await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == id);
        var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);

        return new ProjectDto
        {
            Id = project.Id,
            Key = project.Key,
            Name = project.Name,
            Description = project.Description,
            AvatarUrl = project.AvatarUrl,
            OwnerId = project.OwnerId,
            Owner = project.Owner != null ? new UserDto
            {
                Id = project.Owner.Id,
                Email = project.Owner.Email ?? string.Empty,
                FirstName = project.Owner.FirstName,
                LastName = project.Owner.LastName,
                AvatarUrl = project.Owner.AvatarUrl
            } : null,
            CreatedAt = project.CreatedAt,
            MemberCount = memberCount,
            TaskCount = taskCount
        };
    }

    public async Task<List<ProjectDto>> GetUserProjectsAsync(string userId)
    {
        var projectIds = await _context.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        var projects = await _context.Projects
            .Include(p => p.Owner)
            .Where(p => projectIds.Contains(p.Id))
            .ToListAsync();

        var result = new List<ProjectDto>();
        foreach (var project in projects)
        {
            var memberCount = await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == project.Id);
            var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == project.Id);

            result.Add(new ProjectDto
            {
                Id = project.Id,
                Key = project.Key,
                Name = project.Name,
                Description = project.Description,
                AvatarUrl = project.AvatarUrl,
                OwnerId = project.OwnerId,
                Owner = project.Owner != null ? new UserDto
                {
                    Id = project.Owner.Id,
                    Email = project.Owner.Email ?? string.Empty,
                    FirstName = project.Owner.FirstName,
                    LastName = project.Owner.LastName,
                    AvatarUrl = project.Owner.AvatarUrl
                } : null,
                CreatedAt = project.CreatedAt,
                MemberCount = memberCount,
                TaskCount = taskCount
            });
        }

        return result;
    }

    public async Task<ProjectDto> UpdateAsync(string id, string userId, UpdateProjectRequest request)
    {
        if (!await IsProjectAdminAsync(id, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can update projects");
        }

        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found");
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id, userId) ?? throw new InvalidOperationException("Failed to update project");
    }

    public async Task DeleteAsync(string id, string userId)
    {
        if (!await IsProjectAdminAsync(id, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can delete projects");
        }

        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsProjectMemberAsync(string projectId, string userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<bool> IsProjectAdminAsync(string projectId, string userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && 
                           pm.UserId == userId && 
                           pm.Role == ProjectRole.Admin);
    }
}

