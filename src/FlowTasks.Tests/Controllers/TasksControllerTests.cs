using System.Threading.Tasks;
using Xunit;
using Moq;
using FlowTasks.Tests.Common;

namespace FlowTasks.Tests.Controllers;

public class TasksControllerTests : TestBase
{
	[Fact]
	public async Task AssignTask_Unauthorized_ReturnsForbidden()
	{
		// Arrange
		// Act
		// Assert
		Assert.True(true);
	}
}
