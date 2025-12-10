namespace FlowTasks.Application.DTOs;

public class ProjectDto
{
    public string Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public UserDto? Owner { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
}

