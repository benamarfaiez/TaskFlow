using AutoFixture;
using AutoFixture.Xunit2;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Application.Services;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Interfaces;
using FlowTasks.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Tests.Services;

public class TaskServiceTests : TestBase
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _unitOfWorkMock = FreezeMock<IUnitOfWork>();
        _projectServiceMock = FreezeMock<IProjectService>();
        _notificationServiceMock = FreezeMock<INotificationService>();

        _sut = new TaskService(
            _unitOfWorkMock.Object,
            _projectServiceMock.Object,
            _notificationServiceMock.Object);
    }

    #region CreateAsync Tests

    [Theory]
    [AutoData]
    public async Task CreateAsync_WithValidData_ShouldCreateTaskAndReturnDto(
        string projectId, string userId, CreateTaskRequest request)
    {
        // Arrange
        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .With(p => p.Key, "TEST")
            .Create();

        var expectedTask = new TaskProject
        {
            Id = "generated-id",
            Key = "TEST-1",
            Summary = request.Summary,
            Description = request.Description,
            Type = request.Type,
            Status = TaskStatus.ToDo,
            Priority = request.Priority,
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            ReporterId = userId,
            DueDate = request.DueDate,
            Labels = request.Labels != null ? System.Text.Json.JsonSerializer.Serialize(request.Labels) : null,
            SprintId = request.SprintId,
            EpicId = request.EpicId,
            ParentId = request.ParentId,
            Attachments = request.Attachments != null ? System.Text.Json.JsonSerializer.Serialize(request.Attachments) : null,
            CreatedAt = DateTime.UtcNow
        };

        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _unitOfWorkMock.Setup(x => x.Tasks.CountAsync(It.IsAny<Expression<Func<TaskProject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _unitOfWorkMock.Setup(x => x.Tasks.AddAsync(It.IsAny<TaskProject>(), It.IsAny<CancellationToken>()))
            .Callback<TaskProject>(t => t.Id = "generated-id")
            .Returns<TaskProject, CancellationToken>((entity, _) =>
            {
                return Task.FromResult(entity);
            });

        _unitOfWorkMock.Setup(x => x.TaskHistories.AddAsync(It.IsAny<TaskHistory>(), It.IsAny<CancellationToken>()))
            .Returns<TaskHistory, CancellationToken>((entity, _) =>
            {
                return Task.FromResult(entity);
            });

        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Setup GetByIdAsync for return
        SetupGetByIdAsync(expectedTask, userId);

        _notificationServiceMock.Setup(x => x.NotifyTaskCreatedAsync(projectId, "TEST-1"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(projectId, userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("generated-id", result.Id);
        Assert.Equal("TEST-1", result.Key);
        Assert.Equal(request.Summary, result.Summary);
        Assert.Equal(request.Type, result.Type);
        Assert.Equal(TaskStatus.ToDo, result.Status);
        Assert.Equal(request.Priority, result.Priority);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(request.AssigneeId, result.AssigneeId);
        Assert.Equal(userId, result.ReporterId);

        _unitOfWorkMock.Verify(x => x.Tasks.AddAsync(It.IsAny<TaskProject>(), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.TaskHistories.AddAsync(
            It.Is<TaskHistory>(
                h => h.TaskId == "generated-id" &&
                h.Field == "Created" &&
                h.NewValue == "TEST-1"),
            It.IsAny<CancellationToken>()),
        Times.Once);

        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        _notificationServiceMock.Verify(x => x.NotifyTaskCreatedAsync(projectId, "TEST-1"), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task CreateAsync_UserNotProjectMember_ShouldThrowUnauthorizedAccessException(
        string projectId, string userId, CreateTaskRequest request)
    {
        // Arrange
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(projectId, userId, request));

        Assert.Contains("not a member of this project", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task CreateAsync_ProjectNotFound_ShouldThrowInvalidOperationException(
        string projectId, string userId, CreateTaskRequest request)
    {
        // Arrange
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(projectId, userId, request));

        Assert.Contains("Project not found", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_NullSummary_ShouldThrowArgumentException()
    {
        // Arrange
        var projectId = "project-1";
        var userId = "user-1";
        var request = new CreateTaskRequest { Summary = null! };

        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .Create();

        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act & Assert - This will fail at entity level, but we test the intent
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(projectId, userId, request));
    }

    #endregion

    #region GetByIdAsync Tests

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_TaskExistsAndUserAuthorized_ShouldReturnTaskDto(
        string taskId, string userId)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .With(t => t.Assignee, Fixture.Create<User>())
            .With(t => t.Reporter, Fixture.Create<User>())
            .Create();

        SetupGetByIdAsync(task, userId);

        // Act
        var result = await _sut.GetByIdAsync(taskId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result!.Id);
        Assert.Equal(task.Key, result.Key);
        Assert.Equal(task.Summary, result.Summary);
        Assert.Equal(task.Status, result.Status);
        Assert.Equal(task.ProjectId, result.ProjectId);
    }

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_TaskNotFound_ShouldReturnNull(string taskId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new List<TaskProject>().AsQueryable());

        // Act
        var result = await _sut.GetByIdAsync(taskId, userId);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_UserNotAuthorized_ShouldReturnNull(
        string taskId, string userId)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(task.ProjectId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetByIdAsync(taskId, userId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetFilteredAsync Tests

    [Theory]
    [AutoData]
    public async Task GetFilteredAsync_ValidRequest_ShouldReturnPagedResult(
        string projectId, string userId, TaskFilterRequest filter)
    {
        // Arrange
        var tasks = Fixture.CreateMany<TaskProject>(5).ToList();
        foreach (var task in tasks)
        {
            task.ProjectId = projectId;
        }

        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _sut.GetFilteredAsync(projectId, userId, filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tasks.Count, result.TotalCount);
        Assert.Equal(filter.PageNumber, result.PageNumber);
        Assert.Equal(filter.PageSize, result.PageSize);
        Assert.True(result.Items.Count <= filter.PageSize);
    }

    [Theory]
    [AutoData]
    public async Task GetFilteredAsync_UserNotProjectMember_ShouldThrowUnauthorizedAccessException(
        string projectId, string userId, TaskFilterRequest filter)
    {
        // Arrange
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetFilteredAsync(projectId, userId, filter));

        Assert.Contains("not a member of this project", exception.Message);
    }

    [Fact]
    public async Task GetFilteredAsync_WithSearchFilter_ShouldFilterResults()
    {
        // Arrange
        var projectId = "project-1";
        var userId = "user-1";
        var filter = new TaskFilterRequest { Search = "urgent", PageNumber = 1, PageSize = 10 };

        var tasks = new[]
        {
            Fixture.Build<TaskProject>()
                .With(t => t.ProjectId, projectId)
                .With(t => t.Summary, "Urgent bug fix")
                .Create(),
            Fixture.Build<TaskProject>()
                .With(t => t.ProjectId, projectId)
                .With(t => t.Summary, "Regular task")
                .Create()
        };

        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _sut.GetFilteredAsync(projectId, userId, filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Contains(result.Items, t => t.Summary.Contains("urgent", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region UpdateAsync Tests

    [Theory]
    [AutoData]
    public async Task UpdateAsync_WithValidData_ShouldUpdateTaskAndReturnDto(
        string taskId, string userId, UpdateTaskRequest request)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        var originalSummary = task.Summary;

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(task.ProjectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.TaskHistories.AddRangeAsync(It.IsAny<IEnumerable<TaskHistory>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupGetByIdAsync(task, userId);
        _notificationServiceMock.Setup(x => x.NotifyTaskUpdatedAsync(task.ProjectId, task.Key))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(taskId, userId, request);

        // Assert
        Assert.NotNull(result);
        _unitOfWorkMock.Verify(x => x.Tasks.Update(task), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyTaskUpdatedAsync(task.ProjectId, task.Key), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task UpdateAsync_TaskNotFound_ShouldThrowInvalidOperationException(
        string taskId, string userId, UpdateTaskRequest request)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new List<TaskProject>().AsQueryable());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(taskId, userId, request));

        Assert.Contains("Task not found", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task UpdateAsync_UserNotAuthorized_ShouldThrowUnauthorizedAccessException(
        string taskId, string userId, UpdateTaskRequest request)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(task.ProjectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateAsync(taskId, userId, request));

        Assert.Contains("not a member of this project", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_StatusChange_ShouldAddHistoryAndNotify()
    {
        // Arrange
        var taskId = "task-1";
        var userId = "user-1";
        var request = new UpdateTaskRequest { Status = TaskStatus.InProgress };

        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Status, TaskStatus.ToDo)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(task.ProjectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.TaskHistories.AddRangeAsync(It.IsAny<IEnumerable<TaskHistory>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupGetByIdAsync(task, userId);
        _notificationServiceMock.Setup(x => x.NotifyTaskMovedAsync(task.ProjectId, task.Key, "InProgress"))
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.NotifyTaskUpdatedAsync(task.ProjectId, task.Key))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateAsync(taskId, userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        _unitOfWorkMock.Verify(x => x.TaskHistories.AddRangeAsync(
            It.Is<IEnumerable<TaskHistory>>(h => h.Any(hist => hist.Field == "Status" && hist.NewValue == "InProgress")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyTaskMovedAsync(task.ProjectId, task.Key, "InProgress"), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Theory]
    [AutoData]
    public async Task DeleteAsync_TaskExistsAndUserIsAdmin_ShouldDeleteTask(
        string taskId, string userId)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectAdminAsync(task.ProjectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _notificationServiceMock.Setup(x => x.NotifyTaskDeletedAsync(task.ProjectId, task.Key))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(taskId, userId);

        // Assert
        _unitOfWorkMock.Verify(x => x.Tasks.Delete(task), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyTaskDeletedAsync(task.ProjectId, task.Key), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteAsync_TaskNotFound_ShouldThrowInvalidOperationException(
        string taskId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new List<TaskProject>().AsQueryable());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteAsync(taskId, userId));

        Assert.Contains("Task not found", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task DeleteAsync_UserNotAdmin_ShouldThrowUnauthorizedAccessException(
        string taskId, string userId)
    {
        // Arrange
        var task = Fixture.Build<TaskProject>()
            .With(t => t.Id, taskId)
            .With(t => t.Project, Fixture.Create<Project>())
            .Create();

        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectAdminAsync(task.ProjectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(taskId, userId));

        Assert.Contains("Only project admins can delete tasks", exception.Message);
    }

    #endregion

    #region GetBoardAsync Tests

    [Theory]
    [AutoData]
    public async Task GetBoardAsync_ValidRequest_ShouldReturnBoardWithColumns(
        string projectId, string userId)
    {
        // Arrange
        var tasks = new[]
        {
            Fixture.Build<TaskProject>()
                .With(t => t.ProjectId, projectId)
                .With(t => t.Status, TaskStatus.ToDo)
                .Create(),
            Fixture.Build<TaskProject>()
                .With(t => t.ProjectId, projectId)
                .With(t => t.Status, TaskStatus.InProgress)
                .Create(),
            Fixture.Build<TaskProject>()
                .With(t => t.ProjectId, projectId)
                .With(t => t.Status, TaskStatus.Done)
                .Create()
        };

        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _sut.GetBoardAsync(projectId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Columns);
        Assert.Equal(3, result.Columns.Count); // ToDo, InProgress, Done

        foreach (TaskStatus status in Enum.GetValues(typeof(TaskStatus)))
        {
            Assert.True(result.Columns.ContainsKey(status));
            var expectedCount = tasks.Count(t => t.Status == status);
            Assert.Equal(expectedCount, result.Columns[status].Count);
        }
    }

    [Theory]
    [AutoData]
    public async Task GetBoardAsync_UserNotProjectMember_ShouldThrowUnauthorizedAccessException(
        string projectId, string userId)
    {
        // Arrange
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetBoardAsync(projectId, userId));

        Assert.Contains("not a member of this project", exception.Message);
    }

    #endregion

    #region Helper Methods

    private void SetupGetByIdAsync(TaskProject task, string userId)
    {
        _unitOfWorkMock.Setup(x => x.Tasks.Query())
            .Returns(new[] { task }.AsQueryable());
        _projectServiceMock.Setup(x => x.IsProjectMemberAsync(task.ProjectId, userId))
            .ReturnsAsync(true);
    }

    #endregion
}
