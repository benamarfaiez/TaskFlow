using AutoFixture;
using AutoFixture.AutoMoq;
using FlowTasks.API.Controllers;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Controllers;

public class UsersControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _userServiceMock = _fixture.Freeze<Mock<IUserService>>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_WithAuthenticatedUser_ReturnsOkWithProfile()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var expectedProfile = _fixture.Create<UserDto>();

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.GetProfileAsync(userId))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedProfile, okResult.Value);
        _userServiceMock.Verify(x => x.GetProfileAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _userServiceMock.Verify(x => x.GetProfileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetProfile_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.GetProfileAsync(userId))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOkWithUpdatedProfile()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<UserDto>();
        var expectedProfile = _fixture.Create<UserDto>();

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, request))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedProfile, okResult.Value);
        _userServiceMock.Verify(x => x.UpdateProfileAsync(userId, request), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<UserDto>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _userServiceMock.Verify(x => x.UpdateProfileAsync(
            It.IsAny<string>(),
            It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<UserDto>();
        var errorMessage = "Email already exists";

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<UserDto>();
        var errorMessage = "Invalid email format";

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<UserDto>();
        var errorMessage = "Name cannot be empty";

        SetupAuthenticatedUser(userId);
        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithUsersList()
    {
        // Arrange
        var expectedUsers = _fixture.CreateMany<UserDto>(10).ToList();

        _userServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(expectedUsers.Count, users.Count);
        _userServiceMock.Verify(x => x.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(new List<UserDto>());

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetAllUsers_WithMultipleUsers_ReturnsAllUsers()
    {
        // Arrange
        var user1 = _fixture.Build<UserDto>()
            .With(u => u.Email, "user1@example.com")
            .Create();
        var user2 = _fixture.Build<UserDto>()
            .With(u => u.Email, "user2@example.com")
            .Create();
        var user3 = _fixture.Build<UserDto>()
            .With(u => u.Email, "user3@example.com")
            .Create();
        var expectedUsers = new List<UserDto> { user1, user2, user3 };

        _userServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(3, users.Count);
        Assert.Contains(users, u => u.Email == "user1@example.com");
        Assert.Contains(users, u => u.Email == "user2@example.com");
        Assert.Contains(users, u => u.Email == "user3@example.com");
    }

    #endregion

    #region GetProjectMembers Tests

    [Fact]
    public async Task GetProjectMembers_WithValidProjectId_ReturnsOkWithMembers()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var expectedMembers = _fixture.CreateMany<UserDto>(5).ToList();

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ReturnsAsync(expectedMembers);

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var members = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(expectedMembers.Count, members.Count);
        _userServiceMock.Verify(x => x.GetProjectMembersAsync(projectId), Times.Once);
    }

    [Fact]
    public async Task GetProjectMembers_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var projectId = _fixture.Create<string>();

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ReturnsAsync(new List<UserDto>());

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var members = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Empty(members);
    }

    [Fact]
    public async Task GetProjectMembers_WhenProjectNotFound_ReturnsBadRequest()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var errorMessage = "Project not found";

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetProjectMembers_WithInvalidProjectId_ReturnsBadRequest()
    {
        // Arrange
        var projectId = "invalid-id";
        var errorMessage = "Invalid project ID format";

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetProjectMembers_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var errorMessage = "Database connection failed";

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetProjectMembers_WithMultipleMembers_ReturnsAllMembers()
    {
        var projectId = _fixture.Create<string>();
        var owner = _fixture.Create<UserDto>();
        var member1 = _fixture.Create<UserDto>();
        var member2 = _fixture.Create<UserDto>();
        var expectedMembers = new List<UserDto> { owner, member1, member2 };

        _userServiceMock.Setup(x => x.GetProjectMembersAsync(projectId))
            .ReturnsAsync(expectedMembers);

        // Act
        var result = await _controller.GetProjectMembers(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var members = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(3, members.Count);
        Assert.All(members, m => Assert.NotNull(m));
    }

    #endregion

    #region Helper Methods

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

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };
    }

    #endregion
}