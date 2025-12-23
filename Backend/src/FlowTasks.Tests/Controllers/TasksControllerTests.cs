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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Controllers;

public class TasksControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _taskServiceMock = _fixture.Freeze<Mock<ITaskService>>();
        _controller = new TasksController(_taskServiceMock.Object);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithAuthenticatedUser_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskRequest>();
        var expectedTask = _fixture.Create<TaskDto>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        Assert.Equal(projectId, createdResult.RouteValues["projectId"]);
        Assert.Equal(expectedTask.Id, createdResult.RouteValues["id"]);
        Assert.Equal(expectedTask, createdResult.Value);
        _taskServiceMock.Verify(x => x.CreateAsync(projectId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _taskServiceMock.Verify(x => x.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreateTaskRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskRequest>();
        var errorMessage = "Project not found";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Create_WithInvalidSprintId_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskRequest>();
        var errorMessage = "Sprint not found";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.CreateAsync(projectId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(projectId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetTasks Tests

    [Fact]
    public async Task GetTasks_WithAuthenticatedUser_ReturnsOkWithPagedResult()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var filter = _fixture.Create<TaskFilterRequest>();
        var expectedResult = _fixture.Create<PagedResult<TaskDto>>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetFilteredAsync(projectId, userId, filter))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTasks(projectId, filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TaskDto>>(okResult.Value);
        Assert.Equal(expectedResult.TotalCount, pagedResult.TotalCount);
        _taskServiceMock.Verify(x => x.GetFilteredAsync(projectId, userId, filter), Times.Once);
    }

    [Fact]
    public async Task GetTasks_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var filter = _fixture.Create<TaskFilterRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetTasks(projectId, filter);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _taskServiceMock.Verify(x => x.GetFilteredAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TaskFilterRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetTasks_WithEmptyResult_ReturnsEmptyPagedResult()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var filter = _fixture.Create<TaskFilterRequest>();
        var emptyResult = new PagedResult<TaskDto>
        {
            Items = new List<TaskDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetFilteredAsync(projectId, userId, filter))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _controller.GetTasks(projectId, filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TaskDto>>(okResult.Value);
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetTasks_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var filter = _fixture.Create<TaskFilterRequest>();
        var errorMessage = "Invalid filter parameters";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetFilteredAsync(projectId, userId, filter))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetTasks(projectId, filter);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithTask()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var expectedTask = _fixture.Create<TaskDto>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetByIdAsync(taskId, userId))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.GetById(projectId, taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedTask, okResult.Value);
        _taskServiceMock.Verify(x => x.GetByIdAsync(taskId, userId), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetByIdAsync(taskId, userId))
            .ReturnsAsync((TaskDto)null);

        // Act
        var result = await _controller.GetById(projectId, taskId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetById(projectId, taskId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _taskServiceMock.Verify(x => x.GetByIdAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithUpdatedTask()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateTaskRequest>();
        var expectedTask = _fixture.Create<TaskDto>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.UpdateAsync(taskId, userId, request))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.Update(projectId, taskId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedTask, okResult.Value);
        _taskServiceMock.Verify(x => x.UpdateAsync(taskId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Update_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateTaskRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Update(projectId, taskId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _taskServiceMock.Verify(x => x.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<UpdateTaskRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateTaskRequest>();
        var errorMessage = "Task not found";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.UpdateAsync(taskId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(projectId, taskId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Update_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<UpdateTaskRequest>();
        var errorMessage = "Invalid status transition";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.UpdateAsync(taskId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(projectId, taskId, request);

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
        var taskId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.DeleteAsync(taskId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(projectId, taskId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _taskServiceMock.Verify(x => x.DeleteAsync(taskId, userId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Delete(projectId, taskId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _taskServiceMock.Verify(x => x.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "Not authorized to delete this task";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.DeleteAsync(taskId, userId))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Delete(projectId, taskId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "Task not found";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.DeleteAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(projectId, taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetBoard Tests

    [Fact]
    public async Task GetBoard_WithAuthenticatedUser_ReturnsOkWithBoard()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var expectedBoard = _fixture.Create<BoardDto>();

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetBoardAsync(projectId, userId))
            .ReturnsAsync(expectedBoard);

        // Act
        var result = await _controller.GetBoard(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedBoard, okResult.Value);
        _taskServiceMock.Verify(x => x.GetBoardAsync(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task GetBoard_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetBoard(projectId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _taskServiceMock.Verify(x => x.GetBoardAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetBoard_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var errorMessage = "Project not found";

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetBoardAsync(projectId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetBoard(projectId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetBoard_WithEmptyBoard_ReturnsOkWithEmptyColumns()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var projectId = _fixture.Create<string>();
        var emptyBoard = new BoardDto
        {
            Columns = new Dictionary<Domain.Enums.TaskStatus, List<TaskDto>>()
        };

        SetupAuthenticatedUser(userId);
        _taskServiceMock.Setup(x => x.GetBoardAsync(projectId, userId))
            .ReturnsAsync(emptyBoard);

        // Act
        var result = await _controller.GetBoard(projectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var board = Assert.IsType<BoardDto>(okResult.Value);
        Assert.Empty(board.Columns);
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