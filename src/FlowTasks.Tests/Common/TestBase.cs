using AutoFixture;
using AutoFixture.AutoMoq;
using FlowTasks.Domain.Entities;
using FlowTasks.Tests.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowTasks.Tests.Common;

public abstract class TestBase
{
    protected readonly IFixture Fixture;

    protected TestBase()
    {
        Fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        // Ajoutez ici d'autres setups communs si besoin
    }

    protected Mock<T> FreezeMock<T>() where T : class
        => Fixture.Freeze<Mock<T>>();

    // Helper to create an in-memory DbContextOptions<TContext>
    protected DbContextOptions<TContext> CreateInMemoryOptions<TContext>(string dbName = null)
        where TContext : DbContext
    {
        return TestDatabaseHelper.CreateInMemoryOptions<TContext>(dbName);
    }
}