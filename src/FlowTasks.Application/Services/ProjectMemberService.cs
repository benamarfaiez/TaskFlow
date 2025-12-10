using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class ProjectMemberService : IProjectMemberService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;

    public ProjectMemberService(ApplicationDbContext context, IProjectService projectService)
    {
        _context = context;
        _projectService = projectService;
    }

    public async Task<ProjectMemberDto> AddMemberAsync(string projectId, string userId, AddProjectMemberRequest request)
    {
        if (!await _projectService.IsProjectAdminAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can add members");
        }

        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == request.UserId);

        if (existingMember != null)
        {
            throw new InvalidOperationException("User is already a member of this project");
        }

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        await _context.Entry(member).Reference(m => m.User).LoadAsync();

        return new ProjectMemberDto
        {
            Id = member.Id,
            ProjectId = member.ProjectId,
            User = new UserDto
            {
                Id = member.User.Id,
                Email = member.User.Email ?? string.Empty,
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                AvatarUrl = member.User.AvatarUrl
            },
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }

    public async Task<List<ProjectMemberDto>> GetMembersAsync(string projectId, string userId)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var members = await _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync();

        return members.Select(m => new ProjectMemberDto
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            User = new UserDto
            {
                Id = m.User.Id,
                Email = m.User.Email ?? string.Empty,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                AvatarUrl = m.User.AvatarUrl
            },
            Role = m.Role,
            JoinedAt = m.JoinedAt
        }).ToList();
    }

    public async Task RemoveMemberAsync(string projectId, string memberId, string userId)
    {
        if (!await _projectService.IsProjectAdminAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can remove members");
        }

        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == memberId);

        if (member == null)
        {
            throw new InvalidOperationException("Member not found");
        }

        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();
    }
}

