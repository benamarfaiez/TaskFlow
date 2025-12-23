using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowTasks.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId}/[controller]")]
[Authorize]
public class TaskHistoryController : ControllerBase
{
    private readonly ITaskHistoryService _historyService;

    public TaskHistoryController(ITaskHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskHistoryDto>>> GetHistory(string taskId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var history = await _historyService.GetByTaskIdAsync(taskId, userId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

