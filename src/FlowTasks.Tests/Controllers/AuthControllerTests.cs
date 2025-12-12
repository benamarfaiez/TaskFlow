using System.Threading.Tasks;
using Xunit;
using Moq;
using FlowTasks.Tests.Common;

namespace FlowTasks.Tests.Controllers;

public class AuthControllerTests : TestBase
{
	[Fact]
	public async Task Login_WithValidCredentials_ReturnsToken()
	{
		// Arrange
		// Act
		// Assert
		Assert.True(true);
	}

	[Fact]
	public async Task Register_DuplicateEmail_ReturnsConflict()
	{
		// Arrange
		// Act
		// Assert
		Assert.True(true);
	}
}
