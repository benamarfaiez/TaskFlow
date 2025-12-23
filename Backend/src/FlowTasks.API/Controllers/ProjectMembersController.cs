using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowTasks.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/[controller]")]
[Authorize]
public class ProjectMembersController : ControllerBase
{
    private readonly IProjectMemberService _memberService;

    public ProjectMembersController(IProjectMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(string projectId, [FromBody] AddProjectMemberRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var member = await _memberService.AddMemberAsync(projectId, userId, request);
            return CreatedAtAction(nameof(GetMembers), new { projectId }, member);
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

    [HttpGet]
    public async Task<ActionResult<List<ProjectMemberDto>>> GetMembers(string projectId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var members = await _memberService.GetMembersAsync(projectId, userId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{memberId}")]
    public async Task<ActionResult> RemoveMember(string projectId, string memberId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            await _memberService.RemoveMemberAsync(projectId, memberId, userId);
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

