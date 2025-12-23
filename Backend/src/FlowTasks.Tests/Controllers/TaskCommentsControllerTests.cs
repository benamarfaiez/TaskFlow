using AutoFixture;
using AutoFixture.AutoMoq;
using FlowTasks.API.Controllers;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FlowTasks.Tests.Controllers;

public class TaskCommentsControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ITaskCommentService> _commentServiceMock;
    private readonly TaskCommentsController _controller;

    public TaskCommentsControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Ignorer les types problématiques d'ASP.NET Core
        _fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _commentServiceMock = _fixture.Freeze<Mock<ITaskCommentService>>();
        _controller = new TaskCommentsController(_commentServiceMock.Object);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithAuthenticatedUser_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var expectedComment = _fixture.Create<TaskCommentDto>();

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.CreateAsync(taskId, userId, request))
            .ReturnsAsync(expectedComment);

        // Act
        var result = await _controller.Create(taskId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetComments), createdResult.ActionName);
        Assert.Equal(taskId, createdResult.RouteValues["taskId"]);
        Assert.Equal(expectedComment, createdResult.Value);
        _commentServiceMock.Verify(x => x.CreateAsync(taskId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Create(taskId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _commentServiceMock.Verify(x => x.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreateTaskCommentRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var errorMessage = "Task not found";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.CreateAsync(taskId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(taskId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Create_WithEmptyComment_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var errorMessage = "Comment content cannot be empty";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.CreateAsync(taskId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(taskId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region GetComments Tests

    [Fact]
    public async Task GetComments_WithAuthenticatedUser_ReturnsOkWithComments()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var expectedComments = _fixture.CreateMany<TaskCommentDto>(5).ToList();

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(expectedComments);

        // Act
        var result = await _controller.GetComments(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var comments = Assert.IsType<List<TaskCommentDto>>(okResult.Value);
        Assert.Equal(expectedComments.Count, comments.Count);
        _commentServiceMock.Verify(x => x.GetByTaskIdAsync(taskId, userId), Times.Once);
    }

    [Fact]
    public async Task GetComments_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var taskId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetComments(taskId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _commentServiceMock.Verify(x => x.GetByTaskIdAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetComments_WhenNoComments_ReturnsEmptyList()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ReturnsAsync(new List<TaskCommentDto>());

        // Act
        var result = await _controller.GetComments(taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var comments = Assert.IsType<List<TaskCommentDto>>(okResult.Value);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var errorMessage = "Task not found";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.GetByTaskIdAsync(taskId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetComments(taskId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithUpdatedComment()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var expectedComment = _fixture.Create<TaskCommentDto>();

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.UpdateAsync(commentId, userId, request))
            .ReturnsAsync(expectedComment);

        // Act
        var result = await _controller.Update(taskId, commentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedComment, okResult.Value);
        _commentServiceMock.Verify(x => x.UpdateAsync(commentId, userId, request), Times.Once);
    }

    [Fact]
    public async Task Update_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Update(taskId, commentId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _commentServiceMock.Verify(x => x.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CreateTaskCommentRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenUserNotCommentAuthor_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var errorMessage = "Not authorized to update this comment";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.UpdateAsync(commentId, userId, request))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Update(taskId, commentId, request);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var errorMessage = "Comment not found";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.UpdateAsync(commentId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(taskId, commentId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Update_WithEmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var request = _fixture.Create<CreateTaskCommentRequest>();
        var errorMessage = "Comment content cannot be empty";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.UpdateAsync(commentId, userId, request))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Update(taskId, commentId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.DeleteAsync(commentId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(taskId, commentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _commentServiceMock.Verify(x => x.DeleteAsync(commentId, userId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.Delete(taskId, commentId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _commentServiceMock.Verify(x => x.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenUserNotCommentAuthor_ReturnsForbid()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var errorMessage = "Not authorized to delete this comment";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.DeleteAsync(commentId, userId))
            .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

        // Act
        var result = await _controller.Delete(taskId, commentId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var errorMessage = "Comment not found";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.DeleteAsync(commentId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(taskId, commentId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    [Fact]
    public async Task Delete_WhenCommentNotFound_ReturnsBadRequest()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var taskId = _fixture.Create<string>();
        var commentId = _fixture.Create<string>();
        var errorMessage = "Comment does not exist";

        SetupAuthenticatedUser(userId);
        _commentServiceMock.Setup(x => x.DeleteAsync(commentId, userId))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(taskId, commentId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value as dynamic;
        Assert.NotNull(value);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };
    }

    #endregion
}