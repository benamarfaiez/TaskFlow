using FlowTasks.Application.DTOs;

namespace FlowTasks.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetProfileAsync(string userId);
    Task<UserDto> UpdateProfileAsync(string userId, UserDto request);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetProjectMembersAsync(string projectId);
}

