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

public class TaskHistoryControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ITaskHistoryService> _historyServiceMock;
    private readonly TaskHistoryController _controller;

    public TaskHistoryControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _historyServiceMock = _fixture.Freeze<Mock<ITaskHistoryService>>();
        _controller = new TaskHistoryController(_historyServiceMock.Object);
    }

    #region GetHistory Tests

    [Fact]
    public async Task GetHistory_WithAuthenticatedUser_ReturnsOkWithHistory()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var expectedHistory = _fixture.CreateMany<TaskHistoryDto>(5).ToList();

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsType<List<TaskHistoryDto>>(okResult.Value);
        Assert.Equal(expectedHistory.Count, history.Count);
        _historyServiceMock.Verify(x => x.GetByTaskIdAsync(taskId, userId), Times.Once);
    }

    [Fact]
    public async Task GetHistory_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var taskId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _historyServiceMock.Verify(x => x.GetByTaskIdAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetHistory_WhenNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(new List<TaskHistoryDto>());

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsType<List<TaskHistoryDto>>(okResult.Value);
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistory_WhenTaskNotFound_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "Task not found";

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetHistory_WhenUserNotAuthorized_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "User not authorized to view this task";

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetHistory_WithMultipleHistoryEntries_ReturnsOrderedList()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var history1 = _fixture.Build<TaskHistoryDto>()
            .With(h => h.CreatedAt, DateTime.UtcNow.AddHours(-3))
            .Create();
        var history2 = _fixture.Build<TaskHistoryDto>()
            .With(h => h.CreatedAt, DateTime.UtcNow.AddHours(-2))
            .Create();
        var history3 = _fixture.Build<TaskHistoryDto>()
            .With(h => h.CreatedAt, DateTime.UtcNow.AddHours(-1))
            .Create();
        var expectedHistory = new List<TaskHistoryDto> { history3, history2, history1 };

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsType<List<TaskHistoryDto>>(okResult.Value);
        Assert.Equal(3, history.Count);
        Assert.True(history[0].CreatedAt > history[1].CreatedAt);
        Assert.True(history[1].CreatedAt > history[2].CreatedAt);
    }

    [Fact]
    public async Task GetHistory_WithDifferentChangeTypes_ReturnsAllTypes()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var statusChange = _fixture.Build<TaskHistoryDto>()
            .With(h => h.Field, "Status")
            .Create();
        var assigneeChange = _fixture.Build<TaskHistoryDto>()
            .With(h => h.Field, "Assignee")
            .Create();
        var priorityChange = _fixture.Build<TaskHistoryDto>()
            .With(h => h.Field, "Priority")
            .Create();
        var expectedHistory = new List<TaskHistoryDto> { statusChange, assigneeChange, priorityChange };

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsType<List<TaskHistoryDto>>(okResult.Value);
        Assert.Equal(3, history.Count);
        Assert.Contains(history, h => h.Field == "Status");
        Assert.Contains(history, h => h.Field == "Assignee");
        Assert.Contains(history, h => h.Field == "Priority");
    }

    [Fact]
    public async Task GetHistory_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "Database connection failed";

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetHistory_WithInvalidTaskId_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = "invalid-id";
        var errorMessage = "Invalid task ID format";

        SetupAuthenticatedUser(userId);
        _historyServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetHistory(taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
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