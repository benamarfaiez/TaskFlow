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

public class SprintRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SprintRepository _repository;
    private readonly IFixture _fixture;

    public SprintRepositoryTests()
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
        _repository = new SprintRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenTaskProjectExists_ReturnsTaskProject()
    {
        // Arrange
        var projectId = "PROJ-001";
        var sprint = new Sprint
        {
            ProjectId = projectId,
            Name = "Sprint 1",
            Goal = "Complete initial setup",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            IsActive = true
        };

        await _context.Sprints.AddAsync(sprint);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.First().ProjectId);
    }

    [Fact]
    public async Task GetActiveSprintByProjectIdAsync_WhenTaskProjectExists_ReturnsTaskProject()
    {
        // Arrange
        var projectId = "PROJ-001";
        var sprint = new Sprint
        {
            ProjectId = projectId,
            Name = "Sprint 1",
            Goal = "Complete initial setup",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            IsActive = true
        };

        await _context.Sprints.AddAsync(sprint);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSprintByProjectIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
    }


}
