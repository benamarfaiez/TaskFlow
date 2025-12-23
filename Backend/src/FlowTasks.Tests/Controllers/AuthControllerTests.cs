using AutoFixture;
using AutoFixture.AutoMoq;
using FlowTasks.API.Controllers;
using FlowTasks.Application.DTOs.Auth;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Controllers;

public class AuthControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _authServiceMock = _fixture.Freeze<Mock<IAuthService>>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var request = _fixture.Create<RegisterRequest>();
        var expectedResponse = _fixture.Create<LoginResponse>();
        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
        _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
    }

    [Fact]
    public async Task Register_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var request = _fixture.Create<RegisterRequest>();
        var errorMessage = "Email already exists";
        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var expectedResponse = _fixture.Create<LoginResponse>();
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var errorMessage = "Invalid credentials";
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var value = unauthorizedResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Login_WhenServiceThrowsGeneralException_ReturnsBadRequest()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var errorMessage = "Service error";
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var request = _fixture.Create<RefreshTokenRequest>();
        var expectedResponse = _fixture.Create<LoginResponse>();
        _authServiceMock.Setup(x => x.RefreshTokenAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<RefreshTokenRequest>();
        var errorMessage = "Invalid refresh token";
        _authServiceMock.Setup(x => x.RefreshTokenAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var value = unauthorizedResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        SetupAuthenticatedUser(userId);
        _authServiceMock.Setup(x => x.LogoutAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _authServiceMock.Verify(x => x.LogoutAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Logout_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        // Act
        var result = await _controller.Logout();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _authServiceMock.Verify(x => x.LogoutAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<ChangePasswordRequest>();
        SetupAuthenticatedUser(userId);
        _authServiceMock.Setup(x => x.ChangePasswordAsync(userId, request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _authServiceMock.Verify(x => x.ChangePasswordAsync(userId, request), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<ChangePasswordRequest>();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<ChangePasswordRequest>();
        var errorMessage = "Old password is incorrect";
        SetupAuthenticatedUser(userId);
        _authServiceMock.Setup(x => x.ChangePasswordAsync(userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }
}