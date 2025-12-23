using AutoFixture;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Repositories;

public class TaskHistoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskHistoryRepository _repository;
    private readonly IFixture _fixture;

    public TaskHistoryRepositoryTests()
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
        _repository = new TaskHistoryRepository(_context);
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
        var taskId = "TASK-001";
        var taskHistory = new TaskHistory
        {
            TaskId = taskId,
            UserId = "USER-001",
            CreatedAt = DateTime.UtcNow
        };

        await _context.TaskHistories.AddAsync(taskHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTaskIdAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskHistory.TaskId, result.First().TaskId);
    }

    [Fact]
    public async Task GetByTaskIdWithUserAsync_WhenTaskProjectExists_ReturnsTaskProject()
    {
        // Arrange
        var taskId = "TASK-001";
        var taskHistory = new TaskHistory
        {
            TaskId = taskId,
            UserId = "USER-001",
            CreatedAt = DateTime.UtcNow,
        };
        var user = new User
        {
            Id = "USER-001",
            UserName = "testuser",
            Email = "test@test.test",
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.TaskHistories.AddAsync(taskHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTaskIdWithUserAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskHistory.TaskId, result.First().TaskId);
    }

}
