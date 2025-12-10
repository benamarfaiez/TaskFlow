using FlowTasks.Application.DTOs.Auth;

namespace FlowTasks.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string userId);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
}

