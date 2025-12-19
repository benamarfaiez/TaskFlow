using AutoFixture;
using AutoFixture.AutoMoq;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Application.Services;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Services;

public class SprintServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly SprintService _sprintService;

    public SprintServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _projectServiceMock = new Mock<IProjectService>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();

        _unitOfWorkMock.Setup(x => x.Sprints).Returns(_sprintRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Tasks).Returns(_taskRepositoryMock.Object);

        _sprintService = new SprintService(
            _unitOfWorkMock.Object,
            _projectServiceMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithNonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var request = new CreateSprintRequest
        {
            Name = "Sprint 1",
            Goal = "Complete feature X",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sprintService.CreateAsync(projectId, userId, request));

        Assert.Equal("Only project admins can create sprints", exception.Message);
        _sprintRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Sprint>(), default), Times.Never);
    }
    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidSprintAndMember_ShouldReturnSprintDto()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Sprint 1",
            Goal = "Goal 1",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(sprint);

        _projectServiceMock
            .Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<TaskProject, bool>>>(), default))
            .ReturnsAsync(5);

        // Act
        var result = await _sprintService.GetByIdAsync(sprintId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sprint.Id, result.Id);
        Assert.Equal(sprint.Name, result.Name);
        Assert.Equal(sprint.Goal, result.Goal);
        Assert.Equal(5, result.TaskCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentSprint_ShouldReturnNull()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _sprintService.GetByIdAsync(sprintId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonMember_ShouldReturnNull()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Sprint 1"
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(sprint);

        _projectServiceMock
            .Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sprintService.GetByIdAsync(sprintId, userId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByProjectIdAsync Tests

    [Fact]
    public async Task GetByProjectIdAsync_WithNonMember_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _projectServiceMock
            .Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sprintService.GetByProjectIdAsync(projectId, userId));

        Assert.Equal("You are not a member of this project", exception.Message);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithNoSprints_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _projectServiceMock
            .Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);

        _sprintRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Sprint, bool>>>(), default))
            .ReturnsAsync(new List<Sprint>());

        // Act
        var result = await _sprintService.GetByProjectIdAsync(projectId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidAdminUser_ShouldUpdateSprint()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var existingSprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Old Name",
            Goal = "Old Goal",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var request = new CreateSprintRequest
        {
            Name = "Updated Name",
            Goal = "Updated Goal",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(existingSprint);

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(projectId, userId))
            .ReturnsAsync(true);

        _projectServiceMock
            .Setup(x => x.IsProjectMemberAsync(projectId, userId))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<TaskProject, bool>>>(), default))
            .ReturnsAsync(0);

        _unitOfWorkMock
            .Setup(x => x.CompleteAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sprintService.UpdateAsync(sprintId, userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Goal, result.Goal);

    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentSprint_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var request = new CreateSprintRequest
        {
            Name = "Updated Name",
            Goal = "Updated Goal",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync((Sprint?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sprintService.UpdateAsync(sprintId, userId, request));

        Assert.Equal("Sprint not found", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithNonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var existingSprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Old Name"
        };

        var request = new CreateSprintRequest
        {
            Name = "Updated Name",
            Goal = "Updated Goal",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(existingSprint);

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sprintService.UpdateAsync(sprintId, userId, request));

        Assert.Equal("Only project admins can update sprints", exception.Message);
        _sprintRepositoryMock.Verify(x => x.Update(It.IsAny<Sprint>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenUpdateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var existingSprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Old Name"
        };

        var request = new CreateSprintRequest
        {
            Name = "Updated Name",
            Goal = "Updated Goal",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        _sprintRepositoryMock
            .SetupSequence(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(existingSprint)
            .ReturnsAsync((Sprint?)null);

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(projectId, userId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.CompleteAsync(default))
            .ReturnsAsync(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sprintService.UpdateAsync(sprintId, userId, request));

        Assert.Equal("Failed to update sprint", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidAdminUser_ShouldDeleteSprint()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Sprint to Delete"
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(sprint);

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(projectId, userId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.CompleteAsync(default))
            .ReturnsAsync(1);

        // Act
        await _sprintService.DeleteAsync(sprintId, userId);

        // Assert
        _sprintRepositoryMock.Verify(x => x.Delete(sprint), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentSprint_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync((Sprint?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sprintService.DeleteAsync(sprintId, userId));

        Assert.Equal("Sprint not found", exception.Message);
        _sprintRepositoryMock.Verify(x => x.Delete(It.IsAny<Sprint>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithNonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Sprint to Delete"
        };

        _sprintRepositoryMock
            .Setup(x => x.GetByIdAsync(sprintId, default))
            .ReturnsAsync(sprint);

        _projectServiceMock
            .Setup(x => x.IsProjectAdminAsync(projectId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sprintService.DeleteAsync(sprintId, userId));

        Assert.Equal("Only project admins can delete sprints", exception.Message);
        _sprintRepositoryMock.Verify(x => x.Delete(It.IsAny<Sprint>()), Times.Never);
    }

    #endregion
}