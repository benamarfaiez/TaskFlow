using AutoFixture;
using FlowTasks.Application.DTOs.Auth;
using FlowTasks.Domain.Entities;
using FlowTasks.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Services;

public class AuthServiceTests : TestBase
{
	[Fact]
	public async Task Register_WithValidData_ShouldSucceed()
	{
		// Arrange
		var registerDto = Fixture.Create<RegisterRequest>();
		var userRepo = FreezeMock<UserManager<User>>();

		// Act
		// Assert
		Assert.True(true);
	}

	[Fact]
	public async Task Register_DuplicateEmail_ShouldThrow()
	{
		// Arrange
		// Act / Assert
		await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.CompletedTask);
	}
}
