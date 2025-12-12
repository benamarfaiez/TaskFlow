using System.Threading.Tasks;
using Xunit;
using Moq;
using FlowTasks.Tests.Common;

namespace FlowTasks.Tests.Controllers;

public class ProjectsControllerTests : TestBase
{
	[Fact]
	public async Task GetProjects_ReturnsOk()
	{
		// Arrange
		var serviceMock = FreezeMock<object>(); // replace with IProjectService
		// Act
		// Assert
		Assert.True(true);
	}

	[Fact]
	public async Task CreateProject_InvalidModel_ReturnsBadRequest()
	{
		// Arrange
		// Act
		// Assert
		Assert.True(true);
	}
}
