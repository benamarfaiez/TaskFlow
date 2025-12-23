using FlowTasks.Domain.Enums;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Application.DTOs;

public class BoardDto
{
    public Dictionary<TaskStatus, List<TaskDto>> Columns { get; set; } = new();
}

