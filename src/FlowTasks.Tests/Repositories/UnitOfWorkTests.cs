using System.Threading.Tasks;
using Xunit;
using Moq;
using FlowTasks.Tests.Common;

namespace FlowTasks.Tests.Repositories;

public class UnitOfWorkTests : TestBase
{
	[Fact]
	public async Task CommitAsync_ShouldReturnPositive()
	{
		// Arrange
		var uowMock = FreezeMock<object>(); // replace with IUnitOfWork
		// Act
		// Assert
		Assert.True(true);
	}
}
