using FlowTasks.Application.DTOs;
using FlowTasks.Application.DTOs.Auth;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FlowTasks.Application.Services;

public class AuthService(UserManager<User> userManager, IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthService> _logger = logger;
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        Log.Information("M__LoginAsync "+ request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        _logger.LogInformation("FirstName: " + user.FirstName+ "Email: " + user.Email);

        var token = GenerateJwtToken(user);
        _logger.LogInformation("token : " + token);

        var refreshToken = GenerateRefreshToken();
        _logger.LogInformation("refreshToken : " + refreshToken);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _logger.LogInformation("RefreshTokenExpiryTime: " + user.RefreshTokenExpiryTime);
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("user LastName: " + user.LastName);

        var userDto = MapToUserDto(user);
        _logger.LogInformation("user dto LastName: " + userDto.LastName);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = userDto
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, "User");

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = MapToUserDto(user)
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.Token);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Invalid token");
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var newToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();
        
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = MapToUserDto(user)
        };
    }

    public async Task LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl
        };
    }
}
