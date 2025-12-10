using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowTasks.Application.Services;

public class ProjectMemberService : IProjectMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectService _projectService;

    public ProjectMemberService(IUnitOfWork unitOfWork, IProjectService projectService)
    {
        _unitOfWork = unitOfWork;
        _projectService = projectService;
    }

    public async Task<ProjectMemberDto> AddMemberAsync(string projectId, string userId, AddProjectMemberRequest request)
    {
        if (!await _projectService.IsProjectAdminAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can add members");
        }

        var existingMember = await _unitOfWork.ProjectMembers.GetByProjectAndUserAsync(projectId, request.UserId);

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

        await _unitOfWork.ProjectMembers.AddAsync(member);
        await _unitOfWork.CompleteAsync();

        // Load User navigation property
        var memberWithUser = await _unitOfWork.ProjectMembers.Query()
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == member.Id);

        return new ProjectMemberDto
        {
            Id = memberWithUser!.Id,
            ProjectId = memberWithUser.ProjectId,
            User = new UserDto
            {
                Id = memberWithUser.User.Id,
                Email = memberWithUser.User.Email ?? string.Empty,
                FirstName = memberWithUser.User.FirstName,
                LastName = memberWithUser.User.LastName,
                AvatarUrl = memberWithUser.User.AvatarUrl
            },
            Role = memberWithUser.Role,
            JoinedAt = memberWithUser.JoinedAt
        };
    }

    public async Task<List<ProjectMemberDto>> GetMembersAsync(string projectId, string userId)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var members = await _unitOfWork.ProjectMembers.GetByProjectIdWithUserAsync(projectId);

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

        var member = await _unitOfWork.ProjectMembers.Query()
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == memberId);

        if (member == null)
        {
            throw new InvalidOperationException("Member not found");
        }

        _unitOfWork.ProjectMembers.Delete(member);
        await _unitOfWork.CompleteAsync();
    }
}
