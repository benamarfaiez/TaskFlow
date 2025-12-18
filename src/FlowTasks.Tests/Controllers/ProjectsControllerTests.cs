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

public class ProjectsControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _projectServiceMock = _fixture.Freeze<Mock<IProjectService>>();
        _controller = new ProjectsController(_projectServiceMock.Object);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithAuthenticatedUser_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<CreateProjectRequest>();
        var expectedProject = _fixture.Create<ProjectDto>();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.CreateAsync(userId, request))
            .ReturnsAsync(expectedProject);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        Assert.Equal(expectedProject.Id, createdResult.RouteValues["id"]);
        Assert.Equal(expectedProject, createdResult.Value);
        _projectServiceMock.Verify(x => x.CreateAsync(userId, request), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<CreateProjectRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _projectServiceMock.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CreateProjectRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var request = _fixture.Create<CreateProjectRequest>();
        var errorMessage = "Project creation failed";

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.CreateAsync(userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetUserProjects Tests

    [Fact]
    public async Task GetUserProjects_WithAuthenticatedUser_ReturnsOkWithProjects()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var expectedProjects = _fixture.CreateMany<ProjectDto>(3).ToList();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.GetUserProjectsAsync(userId))
            .ReturnsAsync(expectedProjects);

        // Act
        var result = await _controller.GetUserProjects();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsType<List<ProjectDto>>(okResult.Value);
        Assert.Equal(expectedProjects.Count, projects.Count);
        _projectServiceMock.Verify(x => x.GetUserProjectsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserProjects_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetUserProjects();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _projectServiceMock.Verify(x => x.GetUserProjectsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetUserProjects_WhenNoProjects_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.GetUserProjectsAsync(userId))
            .ReturnsAsync(new List<ProjectDto>());

        // Act
        var result = await _controller.GetUserProjects();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsType<List<ProjectDto>>(okResult.Value);
        Assert.Empty(projects);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithProject()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var expectedProject = _fixture.Create<ProjectDto>();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.GetByIdAsync(projectId, userId))
            .ReturnsAsync(expectedProject);

        // Act
        var result = await _controller.GetById(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedProject, okResult.Value);
        _projectServiceMock.Verify(x => x.GetByIdAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.GetByIdAsync(projectId, userId))
            .ReturnsAsync((ProjectDto)null);

        // Act
        var result = await _controller.GetById(projectId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetById(projectId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _projectServiceMock.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithUpdatedProject()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateProjectRequest>();
        var expectedProject = _fixture.Create<ProjectDto>();

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.UpdateAsync(projectId, userId, request))
            .ReturnsAsync(expectedProject);

        // Act
        var result = await _controller.Update(projectId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedProject, okResult.Value);
        _projectServiceMock.Verify(x => x.UpdateAsync(projectId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Update_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateProjectRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Update(projectId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _projectServiceMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateProjectRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenUnauthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateProjectRequest>();
        var errorMessage = "Not authorized to update this project";

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.UpdateAsync(projectId, userId, request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Update(projectId, request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateProjectRequest>();
        var errorMessage = "Update failed";

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.UpdateAsync(projectId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(projectId, request);

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

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.DeleteAsync(projectId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(projectId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _projectServiceMock.Verify(x => x.DeleteAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Delete(projectId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _projectServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenUnauthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var errorMessage = "Not authorized to delete this project";

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.DeleteAsync(projectId, userId))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Delete(projectId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var errorMessage = "Delete failed";

        SetupAuthenticatedUser(userId);
        _projectServiceMock.Setup(x => x.DeleteAsync(projectId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(projectId);

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