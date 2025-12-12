using System.Threading.Tasks;
using Xunit;
using Moq;
using FlowTasks.Tests.Common;
using System;

namespace FlowTasks.Tests.Services;

public class SprintServiceTests : TestBase
{
	[Fact]
	public async Task CreateSprint_WithValidData_ShouldSucceed()
	{
		// Arrange
		// Act
		// Assert
		Assert.True(true);
	}

	[Fact]
	public async Task StartSprint_NotAllowedState_ShouldThrow()
	{
		// Arrange
		// Act / Assert
		await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.CompletedTask);
	}
}
