using AutoFixture;
using AutoFixture.Xunit2;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Application.Services;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Repositories;
using FlowTasks.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Services;

public class ProjectServiceTests : TestBase
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _unitOfWorkMock = FreezeMock<IUnitOfWork>();
        _sut = new ProjectService(_unitOfWorkMock.Object);
    }

    #region CreateAsync Tests

    [Theory]
    [AutoData]
    public async Task CreateAsync_WithValidData_ShouldCreateProjectAndReturnDto(
        string userId, CreateProjectRequest request)
    {
        // Arrange
        var project = new Project
        {
            Id = "generated-id",
            Key = request.Key.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock.Setup(x => x.Projects.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns<Project, CancellationToken>((entity, _) =>
            {
                return Task.FromResult(entity);
            });

        _unitOfWorkMock.Setup(x => x.ProjectMembers.AddAsync(It.IsAny<ProjectMember>(), It.IsAny<CancellationToken>()))
            .Returns<ProjectMember, CancellationToken>((entity, _) =>
            {
                return Task.FromResult(entity);
            });
        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Setup GetByIdAsync for return
        SetupGetByIdAsync(project, userId, 1, 0);

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("generated-id", result.Id);
        Assert.Equal(request.Key.ToUpper(), result.Key);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(userId, result.OwnerId);

        _unitOfWorkMock.Verify(x => x.Projects.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.ProjectMembers.AddAsync(It.Is<ProjectMember>(
            pm => pm.ProjectId == "generated-id" &&
            pm.UserId == userId &&
            pm.Role == ProjectRole.Admin
            ), It.IsAny<CancellationToken>()
         ), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "user-1";
        var request = new CreateProjectRequest { Key = null!, Name = "Test Project" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(userId, request));
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "user-1";
        var request = new CreateProjectRequest { Key = "TEST", Name = "" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(userId, request));
    }

    #endregion

    #region GetByIdAsync Tests

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_ProjectExistsAndUserIsMember_ShouldReturnProjectDto(
        string projectId, string userId)
    {
        // Arrange
        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .With(p => p.Owner, Fixture.Create<User>())
            .Create();

        SetupGetByIdAsync(project, userId, 5, 10);

        // Act
        var result = await _sut.GetByIdAsync(projectId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result!.Id);
        Assert.Equal(project.Key, result.Key);
        Assert.Equal(project.Name, result.Name);
        Assert.Equal(project.Description, result.Description);
        Assert.Equal(project.OwnerId, result.OwnerId);
        Assert.Equal(5, result.MemberCount);
        Assert.Equal(10, result.TaskCount);
    }

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_ProjectNotFound_ShouldReturnNull(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.IsMemberAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetByIdAsync(projectId, userId);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [AutoData]
    public async Task GetByIdAsync_UserNotMember_ShouldReturnNull(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.IsMemberAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetByIdAsync(projectId, userId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserProjectsAsync Tests

    [Theory]
    [AutoData]
    public async Task GetUserProjectsAsync_UserHasProjects_ShouldReturnProjectList(
        string userId)
    {
        // Arrange
        var projectIds = new[] { "project-1", "project-2" };
        var projects = new[]
        {
            Fixture.Build<Project>().With(p => p.Id, "project-1").Create(),
            Fixture.Build<Project>().With(p => p.Id, "project-2").Create()
        };

        _unitOfWorkMock.Setup(x => x.ProjectMembers.Query())
            .Returns(new[] { new ProjectMember { ProjectId = "project-1", UserId = userId },
                           new ProjectMember { ProjectId = "project-2", UserId = userId } }.AsQueryable());

        _unitOfWorkMock.Setup(x => x.Projects.Query())
            .Returns(projects.AsQueryable());

        _unitOfWorkMock.Setup(x => x.ProjectMembers.CountAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _unitOfWorkMock.Setup(x => x.Tasks.CountAsync(
            It.IsAny<Expression<Func<TaskProject, bool>>>(),
            It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(5);

        // Act
        var result = await _sut.GetUserProjectsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == "project-1");
        Assert.Contains(result, p => p.Id == "project-2");

        foreach (var project in result)
        {
            Assert.Equal(3, project.MemberCount);
            Assert.Equal(5, project.TaskCount);
        }
    }

    [Theory]
    [AutoData]
    public async Task GetUserProjectsAsync_UserHasNoProjects_ShouldReturnEmptyList(
        string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.Query())
            .Returns(new List<ProjectMember>().AsQueryable());

        // Act
        var result = await _sut.GetUserProjectsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Theory]
    [AutoData]
    public async Task UpdateAsync_WithValidDataAndUserIsAdmin_ShouldUpdateProjectAndReturnDto(
        string projectId, string userId, UpdateProjectRequest request)
    {
        // Arrange
        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .Create();

        var originalName = project.Name;
        var originalDescription = project.Description;

        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(
            It.IsAny<Expression<Func<ProjectMember, bool>>>(),
            It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(true); // User is admin

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupGetByIdAsync(project, userId, 1, 0);

        // Act
        var result = await _sut.UpdateAsync(projectId, userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, project.Name);
        Assert.Equal(request.Description, project.Description);
        Assert.NotNull(project.UpdatedAt);

        _unitOfWorkMock.Verify(x => x.Projects.Update(project), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task UpdateAsync_ProjectNotFound_ShouldThrowInvalidOperationException(
        string projectId, string userId, UpdateProjectRequest request)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(
            It.IsAny<Expression<Func<ProjectMember, bool>>>(),
            It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(true); // User is admin

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(projectId, userId, request));

        Assert.Contains("Project not found", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task UpdateAsync_UserNotAdmin_ShouldThrowUnauthorizedAccessException(
        string projectId, string userId, UpdateProjectRequest request)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(
            It.IsAny<Expression<Func<ProjectMember, bool>>>(),
            It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(false); // User is not admin

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateAsync(projectId, userId, request));

        Assert.Contains("Only project admins can update projects", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_NullName_ShouldThrowArgumentException()
    {
        // Arrange
        var projectId = "project-1";
        var userId = "user-1";
        var request = new UpdateProjectRequest { Name = null!, Description = "Description" };

        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .Create();

        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(projectId, userId, request));
    }

    #endregion

    #region DeleteAsync Tests

    [Theory]
    [AutoData]
    public async Task DeleteAsync_ProjectExistsAndUserIsAdmin_ShouldDeleteProject(
        string projectId, string userId)
    {
        // Arrange
        var project = Fixture.Build<Project>()
            .With(p => p.Id, projectId)
            .Create();

        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _unitOfWorkMock.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.DeleteAsync(projectId, userId);

        // Assert
        _unitOfWorkMock.Verify(x => x.Projects.Delete(project), Times.Once);
        _unitOfWorkMock.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteAsync_ProjectNotFound_ShouldThrowInvalidOperationException(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // User is admin

        _unitOfWorkMock.Setup(x => x.Projects.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteAsync(projectId, userId));

        Assert.Contains("Project not found", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task DeleteAsync_UserNotAdmin_ShouldThrowUnauthorizedAccessException(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // User is not admin

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(projectId, userId));

        Assert.Contains("Only project admins can delete projects", exception.Message);
    }

    #endregion

    #region IsProjectMemberAsync Tests

    [Theory]
    [AutoData]
    public async Task IsProjectMemberAsync_UserIsMember_ShouldReturnTrue(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.IsMemberAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsProjectMemberAsync(projectId, userId);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [AutoData]
    public async Task IsProjectMemberAsync_UserIsNotMember_ShouldReturnFalse(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.IsMemberAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.IsProjectMemberAsync(projectId, userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsProjectAdminAsync Tests

    [Theory]
    [AutoData]
    public async Task IsProjectAdminAsync_UserIsAdmin_ShouldReturnTrue(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsProjectAdminAsync(projectId, userId);

        // Assert
        Assert.True(result);
        _unitOfWorkMock.Verify(x => x.ProjectMembers.ExistsAsync(
            It.Is<Expression<Func<ProjectMember, bool>>>(
            expr => expr.ToString().Contains("ProjectRole.Admin")), 
            It.IsAny<CancellationToken>()
            ),
        Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task IsProjectAdminAsync_UserIsNotAdmin_ShouldReturnFalse(
        string projectId, string userId)
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.ProjectMembers.ExistsAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.IsProjectAdminAsync(projectId, userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private void SetupGetByIdAsync(Project project, string userId, int memberCount, int taskCount)
    {
        _unitOfWorkMock.Setup(x => x.ProjectMembers.IsMemberAsync(project.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(x => x.Projects.Query())
            .Returns(new[] { project }.AsQueryable());

        _unitOfWorkMock.Setup(x => x.ProjectMembers.CountAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberCount);

        _unitOfWorkMock.Setup(x => x.Tasks.CountAsync(It.IsAny<Expression<Func<TaskProject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskCount);
    }

    #endregion
}
