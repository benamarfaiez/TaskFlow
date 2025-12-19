using Microsoft.EntityFrameworkCore;
using System;

namespace FlowTasks.Tests.Utilities;

public static class TestDatabaseHelper
{
	// Create options for an in-memory database (unique name per test by default)
	public static DbContextOptions<TContext> CreateInMemoryOptions<TContext>(string? dbName = null)
		where TContext : DbContext
	{
		var builder = new DbContextOptionsBuilder<TContext>();
		builder.UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString());
		builder.EnableSensitiveDataLogging();
		return builder.Options;
	}
}
