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

public class SprintsControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ISprintService> _sprintServiceMock;
    private readonly SprintsController _controller;

    public SprintsControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _sprintServiceMock = _fixture.Freeze<Mock<ISprintService>>();
        _controller = new SprintsController(_sprintServiceMock.Object);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithAuthenticatedUser_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var expectedSprint = _fixture.Create<SprintDto>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ReturnsAsync(expectedSprint);

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        Assert.Equal(projectId, createdResult.RouteValues["projectId"]);
        Assert.Equal(expectedSprint.Id, createdResult.RouteValues["id"]);
        Assert.Equal(expectedSprint, createdResult.Value);
        _sprintServiceMock.Verify(x => x.CreateAsync(projectId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _sprintServiceMock.Verify(x => x.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreateSprintRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var errorMessage = "Not authorized to create sprints in this project";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var errorMessage = "Sprint dates overlap with existing sprint";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetSprints Tests

    [Fact]
    public async Task GetSprints_WithAuthenticatedUser_ReturnsOkWithSprints()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var expectedSprints = _fixture.CreateMany<SprintDto>(3).ToList();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.GetByProjectIdAsync(projectId, userId))
            .ReturnsAsync(expectedSprints);

        // Act
        var result = await _controller.GetSprints(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sprints = Assert.IsType<List<SprintDto>>(okResult.Value);
        Assert.Equal(expectedSprints.Count, sprints.Count);
        _sprintServiceMock.Verify(x => x.GetByProjectIdAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task GetSprints_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetSprints(projectId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _sprintServiceMock.Verify(x => x.GetByProjectIdAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetSprints_WhenNoSprints_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.GetByProjectIdAsync(projectId, userId))
            .ReturnsAsync(new List<SprintDto>());

        // Act
        var result = await _controller.GetSprints(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sprints = Assert.IsType<List<SprintDto>>(okResult.Value);
        Assert.Empty(sprints);
    }

    [Fact]
    public async Task GetSprints_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var errorMessage = "Project not found";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.GetByProjectIdAsync(projectId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetSprints(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithSprint()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var expectedSprint = _fixture.Create<SprintDto>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.GetByIdAsync(sprintId, userId))
            .ReturnsAsync(expectedSprint);

        // Act
        var result = await _controller.GetById(projectId, sprintId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedSprint, okResult.Value);
        _sprintServiceMock.Verify(x => x.GetByIdAsync(sprintId, userId), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.GetByIdAsync(sprintId, userId))
            .ReturnsAsync((SprintDto)null);

        // Act
        var result = await _controller.GetById(projectId, sprintId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetById(projectId, sprintId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _sprintServiceMock.Verify(x => x.GetByIdAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithUpdatedSprint()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var expectedSprint = _fixture.Create<SprintDto>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.UpdateAsync(sprintId, userId, request))
            .ReturnsAsync(expectedSprint);

        // Act
        var result = await _controller.Update(projectId, sprintId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedSprint, okResult.Value);
        _sprintServiceMock.Verify(x => x.UpdateAsync(sprintId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Update_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Update(projectId, sprintId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _sprintServiceMock.Verify(x => x.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreateSprintRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var errorMessage = "Not authorized to update this sprint";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.UpdateAsync(sprintId, userId, request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Update(projectId, sprintId, request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var request = _fixture.Create<CreateSprintRequest>();
        var errorMessage = "Sprint not found";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.UpdateAsync(sprintId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(projectId, sprintId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.DeleteAsync(sprintId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(projectId, sprintId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _sprintServiceMock.Verify(x => x.DeleteAsync(sprintId, userId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Delete(projectId, sprintId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _sprintServiceMock.Verify(x => x.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var errorMessage = "Not authorized to delete this sprint";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.DeleteAsync(sprintId, userId))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Delete(projectId, sprintId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var errorMessage = "Cannot delete active sprint";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.DeleteAsync(sprintId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(projectId, sprintId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Delete_WhenSprintHasTasks_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var sprintId = _fixture.Create<string>();
        var errorMessage = "Cannot delete sprint with existing tasks";

        SetupAuthenticatedUser(userId);
        _sprintServiceMock.Setup(x => x.DeleteAsync(sprintId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(projectId, sprintId);

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