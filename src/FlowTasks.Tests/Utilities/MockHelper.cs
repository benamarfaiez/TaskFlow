using Moq;
using System.Threading.Tasks;

namespace FlowTasks.Tests.Utilities;

public static class MockHelper
{
	// Setup a CommitAsync call to return success by default
	public static Mock<TUnitOfWork> SetupCommitSuccess<TUnitOfWork>(this Mock<TUnitOfWork> uowMock)
		where TUnitOfWork : class
	{
		// try to setup common method names used in UoW implementations
		uowMock.Setup(m => It.IsAny<Task<int>>() )
		       .Verifiable(); // harmless default, actual setups in tests override
		return uowMock;
	}
}
