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

public class ProjectRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectRepository _repository;
    private readonly IFixture _fixture;

    public ProjectRepositoryTests()
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
        _repository = new ProjectRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByKeyAsync Tests

    [Fact]
    public async Task GetByKeyAsync_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        var projectKey = "PROJ-001";
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = projectKey,
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByKeyAsync(projectKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectKey, result.Key);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal(project.Name, result.Name);
    }

    [Fact]
    public async Task GetByKeyAsync_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Arrange
        var projectKey = "NONEXISTENT";

        // Act
        var result = await _repository.GetByKeyAsync(projectKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKeyAsync_WithMultipleProjects_ReturnsCorrectProject()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project
            {
                Id = _fixture.Create<string>(),
                Key = "PROJ-001",
                Name = "Project 1",
                Description = "Description 1",
                OwnerId = _fixture.Create<string>(),
                CreatedAt = DateTime.UtcNow
            },
            new Project
            {
                Id = _fixture.Create<string>(),
                Key = "PROJ-002",
                Name = "Project 2",
                Description = "Description 2",
                OwnerId = _fixture.Create<string>(),
                CreatedAt = DateTime.UtcNow
            },
            new Project
            {
                Id = _fixture.Create<string>(),
                Key = "PROJ-003",
                Name = "Project 3",
                Description = "Description 3",
                OwnerId = _fixture.Create<string>(),
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Projects.AddRangeAsync(projects);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByKeyAsync("PROJ-002");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PROJ-002", result.Key);
        Assert.Equal("Project 2", result.Name);
    }

    [Fact]
    public async Task GetByKeyAsync_IsCaseSensitive()
    {
        // Arrange
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByKeyAsync("proj-001");

        // Assert
        // Depending on database collation, this might return null or the project
        // This test documents the expected behavior
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKeyAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var projectKey = "PROJ-001";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _repository.GetByKeyAsync(projectKey, cts.Token));
    }

    #endregion

    #region GetByIdWithDetailsAsync Tests

    [Fact]
    public async Task GetByIdWithDetailsAsync_WhenProjectExists_ReturnsProjectWithOwner()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(owner);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Owner);
        Assert.Equal(owner.Email, result.Owner.Email);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Arrange
        var projectId = _fixture.Create<string>();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(projectId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesMembers()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var member1 = new User
        {
            Id = _fixture.Create<string>(),
            Email = "member1@example.com",
            FirstName = "Member",
            LastName = "One",
            PasswordHash = _fixture.Create<string>()
        };
        var member2 = new User
        {
            Id = _fixture.Create<string>(),
            Email = "member2@example.com",
            FirstName = "Member",
            LastName = "Two",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };
        var projectMembers = new List<ProjectMember>
        {
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = project.Id,
                UserId = member1.Id,
                User = member1,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ProjectMember
            {
                Id = _fixture.Create<string>(),
                ProjectId = project.Id,
                UserId = member2.Id,
                User = member2,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(owner, member1, member2);
        await _context.Projects.AddAsync(project);
        await _context.ProjectMembers.AddRangeAsync(projectMembers);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.Equal(2, result.Members.Count);
        Assert.All(result.Members, m => Assert.NotNull(m.User));
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesTasks()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };
        var tasks = new List<TaskProject>
        {
            new TaskProject
            {
                Id = _fixture.Create<string>(),
                Description = "Description 1",
                ProjectId = project.Id,
                Priority = TaskPriority.Medium,
                CreatedAt = DateTime.UtcNow
            },
            new TaskProject
            {
                Id = _fixture.Create<string>(),
                Description = "Description 2",
                ProjectId = project.Id,
                Priority = TaskPriority.High,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddAsync(owner);
        await _context.Projects.AddAsync(project);
        await _context.Tasks.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Tasks);
        Assert.Equal(2, result.Tasks.Count);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesSprints()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Test Project",
            Description = "Test Description",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };
        var sprints = new List<Sprint>
        {
            new Sprint
            {
                Id = _fixture.Create<string>(),
                Name = "Sprint 1",
                Goal = "Sprint 1 Goal",
                ProjectId = project.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(14),
                CreatedAt = DateTime.UtcNow
            },
            new Sprint
            {
                Id = _fixture.Create<string>(),
                Name = "Sprint 2",
                Goal = "Sprint 2 Goal",
                ProjectId = project.Id,
                StartDate = DateTime.UtcNow.AddDays(14),
                EndDate = DateTime.UtcNow.AddDays(28),
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddAsync(owner);
        await _context.Projects.AddAsync(project);
        await _context.Sprints.AddRangeAsync(sprints);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Sprints);
        Assert.Equal(2, result.Sprints.Count);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesAllRelations()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var member = new User
        {
            Id = _fixture.Create<string>(),
            Email = "member@example.com",
            FirstName = "Member",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Complete Project",
            Description = "Complete Description",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };
        var projectMember = new ProjectMember
        {
            Id = _fixture.Create<string>(),
            ProjectId = project.Id,
            UserId = member.Id,
            User = member,
            Role = ProjectRole.Member,
            JoinedAt = DateTime.UtcNow
        };
        var task = new TaskProject
        {
            Id = _fixture.Create<string>(),
            Description = "Test Description",
            ProjectId = project.Id,
            Priority = TaskPriority.Medium,
            CreatedAt = DateTime.UtcNow
        };
        var sprint = new Sprint
        {
            Id = _fixture.Create<string>(),
            Name = "Test Sprint",
            Goal = "Test Goal",
            ProjectId = project.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddRangeAsync(owner, member);
        await _context.Projects.AddAsync(project);
        await _context.ProjectMembers.AddAsync(projectMember);
        await _context.Tasks.AddAsync(task);
        await _context.Sprints.AddAsync(sprint);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Owner);
        Assert.NotNull(result.Members);
        Assert.Single(result.Members);
        Assert.NotNull(result.Members.First().User);
        Assert.NotNull(result.Tasks);
        Assert.Single(result.Tasks);
        Assert.NotNull(result.Sprints);
        Assert.Single(result.Sprints);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithEmptyCollections_ReturnsProjectWithEmptyLists()
    {
        // Arrange
        var owner = new User
        {
            Id = _fixture.Create<string>(),
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User",
            PasswordHash = _fixture.Create<string>()
        };
        var project = new Project
        {
            Id = _fixture.Create<string>(),
            Key = "PROJ-001",
            Name = "Empty Project",
            Description = "Project with no members, tasks, or sprints",
            OwnerId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(owner);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Owner);
        Assert.NotNull(result.Members);
        Assert.Empty(result.Members);
        Assert.NotNull(result.Tasks);
        Assert.Empty(result.Tasks);
        Assert.NotNull(result.Sprints);
        Assert.Empty(result.Sprints);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var projectId = _fixture.Create<string>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _repository.GetByIdWithDetailsAsync(projectId, cts.Token));
    }

    #endregion
}