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

public class ProjectMembersControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IProjectMemberService> _memberServiceMock;
    private readonly ProjectMembersController _controller;

    public ProjectMembersControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _memberServiceMock = _fixture.Freeze<Mock<IProjectMemberService>>();
        _controller = new ProjectMembersController(_memberServiceMock.Object);
    }

    #region AddMember Tests

    [Fact]
    public async Task AddMember_WithAuthenticatedUser_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<AddProjectMemberRequest>();
        var expectedMember = _fixture.Create<ProjectMemberDto>();

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.AddMemberAsync(projectId, userId, request))
            .ReturnsAsync(expectedMember);

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetMembers), createdResult.ActionName);
        Assert.Equal(projectId, createdResult.RouteValues["projectId"]);
        Assert.Equal(expectedMember, createdResult.Value);
        _memberServiceMock.Verify(x => x.AddMemberAsync(projectId, userId, request), Times.Once);
    }

    [Fact]
    public async Task AddMember_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<AddProjectMemberRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _memberServiceMock.Verify(x => x.AddMemberAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<AddProjectMemberRequest>()), Times.Never);
    }

    [Fact]
    public async Task AddMember_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<AddProjectMemberRequest>();
        var errorMessage = "Not authorized to add members to this project";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.AddMemberAsync(projectId, userId, request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task AddMember_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<AddProjectMemberRequest>();
        var errorMessage = "Member already exists";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.AddMemberAsync(projectId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.AddMember(projectId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetMembers Tests

    [Fact]
    public async Task GetMembers_WithAuthenticatedUser_ReturnsOkWithMembers()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var expectedMembers = _fixture.CreateMany<ProjectMemberDto>(3).ToList();

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.GetMembersAsync(projectId, userId))
            .ReturnsAsync(expectedMembers);

        // Act
        var result = await _controller.GetMembers(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var members = Assert.IsType<List<ProjectMemberDto>>(okResult.Value);
        Assert.Equal(expectedMembers.Count, members.Count);
        _memberServiceMock.Verify(x => x.GetMembersAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task GetMembers_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetMembers(projectId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _memberServiceMock.Verify(x => x.GetMembersAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMembers_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.GetMembersAsync(projectId, userId))
            .ReturnsAsync(new List<ProjectMemberDto>());

        // Act
        var result = await _controller.GetMembers(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var members = Assert.IsType<List<ProjectMemberDto>>(okResult.Value);
        Assert.Empty(members);
    }

    [Fact]
    public async Task GetMembers_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var errorMessage = "Project not found";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.GetMembersAsync(projectId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetMembers(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region RemoveMember Tests

    [Fact]
    public async Task RemoveMember_WithAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var memberId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.RemoveMemberAsync(projectId, memberId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _memberServiceMock.Verify(x => x.RemoveMemberAsync(projectId, memberId, userId), Times.Once);
    }

    [Fact]
    public async Task RemoveMember_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var memberId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _memberServiceMock.Verify(x => x.RemoveMemberAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMember_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var memberId = _fixture.Create<string>();
        var errorMessage = "Not authorized to remove members from this project";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.RemoveMemberAsync(projectId, memberId, userId))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RemoveMember_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var memberId = _fixture.Create<string>();
        var errorMessage = "Member not found";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.RemoveMemberAsync(projectId, memberId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task RemoveMember_WhenRemovingLastOwner_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var memberId = _fixture.Create<string>();
        var errorMessage = "Cannot remove the last owner of the project";

        SetupAuthenticatedUser(userId);
        _memberServiceMock.Setup(x => x.RemoveMemberAsync(projectId, memberId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.RemoveMember(projectId, memberId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
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