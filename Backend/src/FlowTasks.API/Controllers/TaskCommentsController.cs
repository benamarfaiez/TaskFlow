using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowTasks.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId}/[controller]")]
[Authorize]
public class TaskCommentsController : ControllerBase
{
    private readonly ITaskCommentService _commentService;

    public TaskCommentsController(ITaskCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<ActionResult<TaskCommentDto>> Create(string taskId, [FromBody] CreateTaskCommentRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comment = await _commentService.CreateAsync(taskId, userId, request);
            return CreatedAtAction(nameof(GetComments), new { taskId }, comment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskCommentDto>>> GetComments(string taskId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comments = await _commentService.GetByTaskIdAsync(taskId, userId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{commentId}")]
    public async Task<ActionResult<TaskCommentDto>> Update(string taskId, string commentId, [FromBody] CreateTaskCommentRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comment = await _commentService.UpdateAsync(commentId, userId, request);
            return Ok(comment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{commentId}")]
    public async Task<ActionResult> Delete(string taskId, string commentId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            await _commentService.DeleteAsync(commentId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

