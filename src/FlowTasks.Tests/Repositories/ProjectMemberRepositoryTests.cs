using AutoFixture;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Repositories;

public class ProjectMemberRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectMemberRepository _repository;
    private readonly IFixture _fixture;

    public ProjectMemberRepositoryTests()
    {
        _fixture = new Fixture();

        // Configurer AutoFixture pour éviter les références circulaires
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Créer une base de données en mémoire
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProjectMemberRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByProjectAndUserAsync Tests

    [Fact]
    public async Task GetByProjectAndUserAsync_WhenMemberExists_ReturnsMember()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId,
            UserId = userId,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectAndUserAsync(projectId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(member.Id, result.Id);
    }

    [Fact]
    public async Task GetByProjectAndUserAsync_WhenMemberDoesNotExist_ReturnsNull()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();

        // Act
        var result = await _repository.GetByProjectAndUserAsync(projectId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByProjectAndUserAsync_WithDifferentProjectId_ReturnsNull()
    {
        // Arrange
        var projectId1 = _fixture.Create<string>();
        var projectId2 = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId1,
            UserId = userId,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectAndUserAsync(projectId2, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByProjectAndUserAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _repository.GetByProjectAndUserAsync(projectId, userId, cts.Token));
    }

    #endregion

    #region GetByProjectIdAsync Tests

    [Fact]
    public async Task GetByProjectIdAsync_WhenMembersExist_ReturnsAllMembers()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var members = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _context.ProjectMembers.AddRangeAsync(members);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.All(result, m => Assert.Equal(projectId, m.ProjectId));
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var projectId = _fixture.Create<string>();

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithMultipleProjects_ReturnsOnlyMatchingProject()
    {
        // Arrange
        var projectId1 = _fixture.Create<string>();
        var projectId2 = _fixture.Create<string>();
        var members = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId1,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId1,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId2,
                UserId = _fixture.Create<string>(),
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _context.ProjectMembers.AddRangeAsync(members);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(projectId1, m.ProjectId));
    }

    #endregion

    #region GetByProjectIdWithUserAsync Tests

    [Fact]
    public async Task GetByProjectIdWithUserAsync_WhenMembersExist_ReturnsWithUserIncluded()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var user = new User
        {
            Id = _fixture.Create<string>(),
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = _fixture.Create<string>()
        };
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId,
            UserId = user.Id,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow,
            User = user
        };

        await _context.Users.AddAsync(user);
        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByProjectIdWithUserAsync(projectId);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.NotNull(resultList[0].User);
        Assert.Equal(user.Email, resultList[0].User.Email);
    }

    [Fact]
    public async Task GetByProjectIdWithUserAsync_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var projectId = _fixture.Create<string>();

        // Act
        var result = await _repository.GetByProjectIdWithUserAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByProjectIdWithUserAsync_WithMultipleMembers_IncludesAllUsers()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var user1 = new User
        {
            Id = _fixture.Create<string>(),
            Email = "user1@example.com",
            FirstName = "User",
            LastName = "One",
            PasswordHash = _fixture.Create<string>()
        };
        var user2 = new User
        {
            Id = _fixture.Create<string>(),
            Email = "user2@example.com",
            FirstName = "User",
            LastName = "Two",
            PasswordHash = _fixture.Create<string>()
        };
        var members = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId,
                UserId = user1.Id,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow,
                User = user1
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = projectId,
                UserId = user2.Id,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow,
                User = user2
            }
        };

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.ProjectMembers.AddRangeAsync(members);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByProjectIdWithUserAsync(projectId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, m => Assert.NotNull(m.User));
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasProjects_ReturnsAllProjects()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var members = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = _fixture.Create<string>(),
                UserId = userId,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = _fixture.Create<string>(),
                UserId = userId,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _context.ProjectMembers.AddRangeAsync(members);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(userId, m.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasNoProjects_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleUsers_ReturnsOnlyMatchingUser()
    {
        // Arrange
        var userId1 = _fixture.Create<string>();
        var userId2 = _fixture.Create<string>();
        var members = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = _fixture.Create<string>(),
                UserId = userId1,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = _fixture.Create<string>(),
                UserId = userId1,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = _fixture.Create<string>(),
                UserId = userId2,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _context.ProjectMembers.AddRangeAsync(members);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.Equal(userId1, m.UserId));
    }

    #endregion

    #region IsMemberAsync Tests

    [Fact]
    public async Task IsMemberAsync_WhenUserIsMember_ReturnsTrue()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId,
            UserId = userId,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsMemberAsync(projectId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMemberAsync_WhenUserIsNotMember_ReturnsFalse()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();

        // Act
        var result = await _repository.IsMemberAsync(projectId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsMemberAsync_WhenUserIsOwner_ReturnsTrue()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId,
            UserId = userId,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsMemberAsync(projectId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMemberAsync_WithWrongProjectId_ReturnsFalse()
    {
        // Arrange
        var projectId1 = _fixture.Create<string>();
        var projectId2 = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var member = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = projectId1,
            UserId = userId,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsMemberAsync(projectId2, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsMemberAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var userId = _fixture.Create<string>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _repository.IsMemberAsync(projectId, userId, cts.Token));
    }

    #endregion
}