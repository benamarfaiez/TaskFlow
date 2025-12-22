using AutoFixture;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Repositories;

public class TaskRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskRepository _repository;
    private readonly IFixture _fixture;

    public TaskRepositoryTests()
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
        _repository = new TaskRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByKeyAsync_WhenTaskProjectExists_ReturnsTaskProject()
    {
        // Arrange
        var projectKey = "PROJ-001";
        var taskProject = new TaskProject
        {
            Key = projectKey,
            Summary = "Implémenter l'authentification OAuth2 avec Google",
            Description = "L'utilisateur doit pouvoir se connecter via son compte Google. " +
                  "Utiliser la bibliothèque IdentityServer ou ASP.NET Core Identity. " +
                  "Gérer les scopes nécessaires et le refresh token.",

            Type = TaskType.Story,
            Priority = TaskPriority.High,
            ProjectId = "proj-123",
            AssigneeId = "user-789",
            ReporterId = "user-101",

            DueDate = new DateTime(2025, 12, 31),
            Labels = "authentification, backend, sécurité",
            SprintId = "sprint-15",
            EpicId = "FLOW-300",
        };


        await _context.Tasks.AddAsync(taskProject);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByKeyAsync(projectKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskProject.Key, result.Key);
        Assert.Equal(taskProject.Summary, result.Summary);
        Assert.Equal(taskProject.Description, result.Description);
        Assert.Equal(taskProject.Type, result.Type);
        Assert.Equal(taskProject.Priority, result.Priority);
        Assert.Equal(taskProject.ProjectId, result.ProjectId);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WhenTaskProjectExists_ReturnsTaskProject()
    {
        // Arrange
        var projectKey = "PROJ-001";
        var taskProject = new TaskProject
        {
            Key = projectKey,
            Summary = "Implémenter l'authentification OAuth2 avec Google",
            Description = "L'utilisateur doit pouvoir se connecter via son compte Google. " +
                  "Utiliser la bibliothèque IdentityServer ou ASP.NET Core Identity. " +
                  "Gérer les scopes nécessaires et le refresh token.",

            Type = TaskType.Story,
            Priority = TaskPriority.High,
            ProjectId = "proj-123",

            Project = _fixture.Create<Project>(),
            Assignee = _fixture.Create<User>(),
            Reporter = _fixture.Create<User>(),
            Sprint = _fixture.Create<Sprint>(),
            Epic = _fixture.Create<TaskProject>(),
            Parent = _fixture.Create<TaskProject>(),
            Subtasks = _fixture.CreateMany<TaskProject>(2).ToList(),
            Comments = _fixture.CreateMany<TaskComment>(3).ToList(),
            History = _fixture.CreateMany<TaskHistory>(4).ToList(),

            AssigneeId = "user-789",
            ReporterId = "user-101",

            DueDate = new DateTime(2025, 12, 31),
            Labels = "authentification, backend, sécurité",
            SprintId = "sprint-15",
            EpicId = "FLOW-300",
        };


        await _context.Tasks.AddAsync(taskProject);
        await _context.SaveChangesAsync();
        var task = await _repository.GetByKeyAsync(projectKey);

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(task.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskProject.Key, result.Key);
        Assert.Equal(taskProject.Summary, result.Summary);
        Assert.Equal(taskProject.Description, result.Description);
        Assert.Equal(taskProject.Type, result.Type);
        Assert.Equal(taskProject.Priority, result.Priority);
        Assert.Equal(taskProject.ProjectId, result.ProjectId);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenTaskProjectExists_ReturnsListTaskProject()
    {
        // Arrange
        var projectId = "PROJ-001";

        var listTaskProject = new List<TaskProject> {
            new() {
                ProjectId = projectId,

                Key = "45815",
                Summary = "Implémenter l'authentification OAuth2 avec Google",
                Description = "L'utilisateur doit pouvoir se connecter via son compte Google. " +
                      "Utiliser la bibliothèque IdentityServer ou ASP.NET Core Identity. " +
                      "Gérer les scopes nécessaires et le refresh token.",

                Type = TaskType.Story,
                Priority = TaskPriority.High,

                AssigneeId = "user-789",
                ReporterId = "user-101",

                DueDate = new DateTime(2027, 12, 31),
                Labels = "authentification, backend, sécurité",
                SprintId = "sprint-15",
                EpicId = "FLOW-300",
            },
            new() {
                ProjectId = projectId,

                Key = "89546",
                Summary = "Implémenter de test",

                Type = TaskType.Task,
                Priority = TaskPriority.Medium,

                AssigneeId = "user-499",
                ReporterId = "user-351",

                DueDate = new DateTime(2029, 11, 22),
                Labels = "backend, sécurité",
                SprintId = "sprint-12",
                EpicId = "FLOW-30",
            }
        };
        await _context.Tasks.AddRangeAsync(listTaskProject);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.ToList().Count, listTaskProject.Count);
    }

    [Fact]
    public async Task GetByAssigneeIdAsync_WhenTaskProjectExists_ReturnsListTaskProject()
    {
        // Arrange
        var assigneeId = "5623-001";

        var listTaskProject = new List<TaskProject> {
            new() {
                ProjectId = "8552123",

                Key = "45815",
                Summary = "Implémenter l'authentification OAuth2 avec Google",
                Description = "L'utilisateur doit pouvoir se connecter via son compte Google. " +
                      "Utiliser la bibliothèque IdentityServer ou ASP.NET Core Identity. " +
                      "Gérer les scopes nécessaires et le refresh token.",

                Type = TaskType.Story,
                Priority = TaskPriority.High,

                AssigneeId = assigneeId,
                ReporterId = "user-101",

                DueDate = new DateTime(2027, 12, 31),
                Labels = "authentification, backend, sécurité",
                SprintId = "sprint-15",
                EpicId = "FLOW-300",
            },
            new() {
                ProjectId = "8721",

                Key = "89546",
                Summary = "Implémenter de test",

                Type = TaskType.Task,
                Priority = TaskPriority.Medium,

                AssigneeId = assigneeId,
                ReporterId = "user-351",

                DueDate = new DateTime(2029, 11, 22),
                Labels = "backend, sécurité",
                SprintId = "sprint-12",
                EpicId = "FLOW-30",
            }
        };
        await _context.Tasks.AddRangeAsync(listTaskProject);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAssigneeIdAsync(assigneeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.ToList().Count, listTaskProject.Count);
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenTaskProjectExists_ReturnsListTaskProject()
    {
        // Arrange
        var sprintId = "sprint-999";
        var listTaskProject = new List<TaskProject> {
            new() {
                ProjectId = "5423",

                Key = "45815",
                Summary = "Implémenter l'authentification OAuth2 avec Google",
                Description = "L'utilisateur doit pouvoir se connecter via son compte Google. " +
                      "Utiliser la bibliothèque IdentityServer ou ASP.NET Core Identity. " +
                      "Gérer les scopes nécessaires et le refresh token.",

                Type = TaskType.Story,
                Priority = TaskPriority.High,

                AssigneeId = "user-789",
                ReporterId = "user-101",

                DueDate = new DateTime(2027, 12, 31),
                Labels = "authentification, backend, sécurité",
                SprintId = sprintId,
                EpicId = "FLOW-300",
            },
            new() {
                ProjectId = "7821",

                Key = "89546",
                Summary = "Implémenter de test",

                Type = TaskType.Task,
                Priority = TaskPriority.Medium,

                AssigneeId = "user-499",
                ReporterId = "user-351",

                DueDate = new DateTime(2029, 11, 22),
                Labels = "backend, sécurité",
                SprintId = sprintId,
                EpicId = "FLOW-30",
            }
        };
        await _context.Tasks.AddRangeAsync(listTaskProject);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySprintIdAsync(sprintId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.ToList().Count, listTaskProject.Count);
    }
}
