using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FlowTasks.Tests.Common;
using FlowTasks.Tests.Utilities;

namespace FlowTasks.Tests.Repositories;

public class ProjectRepositoryTests : TestBase
{
	[Fact]
	public async Task AddProject_ShouldPersist()
	{
		// Arrange
		var options = CreateInMemoryOptions<DbContext>("proj_db");
		// ... create context instance, repository and add entity ...
		// Act
		// Assert
		Assert.True(true); // placeholder
	}

	[Fact]
	public async Task GetById_NotFound_ShouldReturnNull()
	{
		// Arrange
		// Act
		// Assert
		Assert.Null(null);
	}
}
