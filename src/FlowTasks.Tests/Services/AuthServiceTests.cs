using FlowTasks.Application.DTOs.Auth;
using FlowTasks.Application.Services;
using FlowTasks.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Configuration du UserManager Mock
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null
           );

        _configurationMock = new Mock<IConfiguration>();

        // Configuration des valeurs JWT
        SetupJwtConfiguration();

        _authService = new AuthService(
            _userManagerMock.Object,
            _configurationMock.Object);
    }

    private void SetupJwtConfiguration()
    {
        _configurationMock.Setup(x => x["Jwt:Key"])
            .Returns("SuperSecretKeyThatIsAtLeast32CharactersLongForHS256");
        _configurationMock.Setup(x => x["Jwt:Issuer"])
            .Returns("TestIssuer");
        _configurationMock.Setup(x => x["Jwt:Audience"])
            .Returns("TestAudience");
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.FirstName, result.User.FirstName);
        Assert.Equal(user.LastName, result.User.LastName);

        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.RefreshToken != null && u.RefreshTokenExpiryTime != null)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldGenerateValidJwtToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        Assert.Equal(user.Id, token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, token.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Smith"
        };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal(request.Email, result.User.Email);
        Assert.Equal(request.FirstName, result.User.FirstName);
        Assert.Equal(request.LastName, result.User.LastName);

        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<User>(u =>
                u.Email == request.Email &&
                u.UserName == request.Email &&
                u.FirstName == request.FirstName &&
                u.LastName == request.LastName &&
                u.EmailConfirmed == true),
            request.Password), Times.Once);

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "User"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenUserCreationFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "weak",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var errors = new[]
        {
            new IdentityError { Description = "Password too weak" },
            new IdentityError { Description = "Password must contain uppercase" }
        };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));

        Assert.Contains("Password too weak", exception.Message);
        Assert.Contains("Password must contain uppercase", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetEmailConfirmedToTrue()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Smith"
        };

        User? capturedUser = null;
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.True(capturedUser.EmailConfirmed);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        // Générer un token valide
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("SuperSecretKeyThatIsAtLeast32CharactersLongForHS256"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-1), // Token expiré
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var request = new RefreshTokenRequest
        {
            Token = tokenString,
            RefreshToken = "valid-refresh-token"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEqual(request.RefreshToken, result.RefreshToken);

        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.RefreshToken != request.RefreshToken)), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRefreshToken_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            RefreshToken = "different-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("SuperSecretKeyThatIsAtLeast32CharactersLongForHS256"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var request = new RefreshTokenRequest
        {
            Token = tokenString,
            RefreshToken = "invalid-refresh-token"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync(request));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredRefreshToken_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Expiré
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("SuperSecretKeyThatIsAtLeast32CharactersLongForHS256"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var request = new RefreshTokenRequest
        {
            Token = tokenString,
            RefreshToken = "valid-refresh-token"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync(request));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("SuperSecretKeyThatIsAtLeast32CharactersLongForHS256"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var request = new RefreshTokenRequest
        {
            Token = tokenString,
            RefreshToken = "some-refresh-token"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync(request));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidUserId_ShouldClearRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            RefreshToken = "some-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.LogoutAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.RefreshToken == null && u.RefreshTokenExpiryTime == null)), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidUserId_ShouldNotThrowException()
    {
        // Arrange
        var userId = "invalid-user-id";
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        await _authService.LogoutAsync(userId);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ShouldChangePassword()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.ChangePasswordAsync(userId, request);

        // Assert
        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidUserId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = "invalid-user-id";
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.ChangePasswordAsync(userId, request));

        Assert.Equal("User not found", exception.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenChangePasswordFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        var errors = new[]
        {
            new IdentityError { Description = "Incorrect password" }
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.ChangePasswordAsync(userId, request));

        Assert.Contains("Incorrect password", exception.Message);
    }

    #endregion

    #region JWT Configuration Tests

    [Fact]
    public async Task LoginAsync_WithMissingJwtKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _configurationMock.Setup(x => x["Jwt:Key"])
            .Returns((string?)null);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync([]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("JWT Key not configured", exception.Message);
    }

    #endregion
}