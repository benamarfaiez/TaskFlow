using System.Text.Json;
using FlowTasks.Application.DTOs;
using FlowTasks.Application.Interfaces;
using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectService _projectService;
    private readonly INotificationService _notificationService;

    public TaskService(
        IUnitOfWork unitOfWork,
        IProjectService projectService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _projectService = projectService;
        _notificationService = notificationService;
    }

    public async Task<TaskDto> CreateAsync(string projectId, string userId, CreateTaskRequest request)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found");
        }

        // Generate task key
        var taskNumber = await _unitOfWork.Tasks.CountAsync(t => t.ProjectId == projectId) + 1;
        var taskKey = $"{project.Key}-{taskNumber}";

        var task = new TaskProject
        {
            Key = taskKey,
            Summary = request.Summary,
            Description = request.Description,
            Type = request.Type,
            Status = TaskStatus.ToDo,
            Priority = request.Priority,
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            ReporterId = userId,
            DueDate = request.DueDate,
            Labels = request.Labels != null ? JsonSerializer.Serialize(request.Labels) : null,
            SprintId = request.SprintId,
            EpicId = request.EpicId,
            ParentId = request.ParentId,
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.CompleteAsync();

        // Add history
        await _unitOfWork.TaskHistories.AddAsync(new TaskHistory
        {
            TaskId = task.Id,
            UserId = userId,
            Field = "Created",
            NewValue = taskKey,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        // SignalR notification
        await _notificationService.NotifyTaskCreatedAsync(projectId, taskKey);

        return await GetByIdAsync(task.Id, userId) ?? throw new InvalidOperationException("Failed to create task");
    }

    public async Task<TaskDto?> GetByIdAsync(string id, string userId)
    {
        var task = await _unitOfWork.Tasks.Query()
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null || !await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            return null;
        }

        return MapToTaskDto(task);
    }

    public async Task<PagedResult<TaskDto>> GetFilteredAsync(string projectId, string userId, TaskFilterRequest filter)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var query = _unitOfWork.Tasks.Query()
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .Where(t => t.ProjectId == projectId);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(t => t.Summary.Contains(filter.Search) || 
                                    (t.Description != null && t.Description.Contains(filter.Search)) ||
                                    t.Key.Contains(filter.Search));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrEmpty(filter.AssigneeId))
        {
            query = query.Where(t => t.AssigneeId == filter.AssigneeId);
        }

        if (!string.IsNullOrEmpty(filter.SprintId))
        {
            query = query.Where(t => t.SprintId == filter.SprintId);
        }

        var totalCount = await query.CountAsync();

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "summary" => filter.SortDescending ? query.OrderByDescending(t => t.Summary) : query.OrderBy(t => t.Summary),
            "priority" => filter.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "status" => filter.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "duedate" => filter.SortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            _ => filter.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };

        var tasks = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<TaskDto>
        {
            Items = tasks.Select(MapToTaskDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<TaskDto> UpdateAsync(string id, string userId, UpdateTaskRequest request)
    {
        var task = await _unitOfWork.Tasks.Query()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (!await _projectService.IsProjectMemberAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var histories = new List<TaskHistory>();

        // Track changes for history
        if (request.Summary != null && request.Summary != task.Summary)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Summary",
                OldValue = task.Summary,
                NewValue = request.Summary,
                CreatedAt = DateTime.UtcNow
            });
            task.Summary = request.Summary;
        }

        if (request.Description != null && request.Description != task.Description)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Description",
                OldValue = task.Description,
                NewValue = request.Description,
                CreatedAt = DateTime.UtcNow
            });
            task.Description = request.Description;
        }

        if (request.Type.HasValue && request.Type.Value != task.Type)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Type",
                OldValue = task.Type.ToString(),
                NewValue = request.Type.Value.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            task.Type = request.Type.Value;
        }

        if (request.Status.HasValue && request.Status.Value != task.Status)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Status",
                OldValue = task.Status.ToString(),
                NewValue = request.Status.Value.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            task.Status = request.Status.Value;
            await _notificationService.NotifyTaskMovedAsync(task.ProjectId, task.Key, task.Status.ToString());
        }

        if (request.Priority.HasValue && request.Priority.Value != task.Priority)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Priority",
                OldValue = task.Priority.ToString(),
                NewValue = request.Priority.Value.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            task.Priority = request.Priority.Value;
        }

        if (request.AssigneeId != task.AssigneeId)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Assignee",
                OldValue = task.AssigneeId ?? "Unassigned",
                NewValue = request.AssigneeId ?? "Unassigned",
                CreatedAt = DateTime.UtcNow
            });
            task.AssigneeId = request.AssigneeId;
            await _notificationService.NotifyUserAssignedAsync(task.ProjectId, task.Key, request.AssigneeId);
        }

        if (request.DueDate != task.DueDate)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "DueDate",
                OldValue = task.DueDate?.ToString() ?? "None",
                NewValue = request.DueDate?.ToString() ?? "None",
                CreatedAt = DateTime.UtcNow
            });
            task.DueDate = request.DueDate;
        }

        if (request.Labels != null)
        {
            var newLabels = JsonSerializer.Serialize(request.Labels);
            if (newLabels != task.Labels)
            {
                histories.Add(new TaskHistory
                {
                    TaskId = task.Id,
                    UserId = userId,
                    Field = "Labels",
                    OldValue = task.Labels ?? "[]",
                    NewValue = newLabels,
                    CreatedAt = DateTime.UtcNow
                });
                task.Labels = newLabels;
            }
        }

        if (request.SprintId != task.SprintId)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Sprint",
                OldValue = task.SprintId?.ToString() ?? "None",
                NewValue = request.SprintId?.ToString() ?? "None",
                CreatedAt = DateTime.UtcNow
            });
            task.SprintId = request.SprintId;
        }

        if (request.EpicId != task.EpicId)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Epic",
                OldValue = task.EpicId?.ToString() ?? "None",
                NewValue = request.EpicId?.ToString() ?? "None",
                CreatedAt = DateTime.UtcNow
            });
            task.EpicId = request.EpicId;
        }

        if (request.ParentId != task.ParentId)
        {
            histories.Add(new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Field = "Parent",
                OldValue = task.ParentId?.ToString() ?? "None",
                NewValue = request.ParentId?.ToString() ?? "None",
                CreatedAt = DateTime.UtcNow
            });
            task.ParentId = request.ParentId;
        }

        if (request.Attachments != null)
        {
            var newAttachments = JsonSerializer.Serialize(request.Attachments);
            if (newAttachments != task.Attachments)
            {
                task.Attachments = newAttachments;
            }
        }

        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tasks.Update(task);

        if (histories.Any())
        {
            await _unitOfWork.TaskHistories.AddRangeAsync(histories);
        }

        await _unitOfWork.CompleteAsync();

        await _notificationService.NotifyTaskUpdatedAsync(task.ProjectId, task.Key);

        return await GetByIdAsync(id, userId) ?? throw new InvalidOperationException("Failed to update task");
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var task = await _unitOfWork.Tasks.Query()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (!await _projectService.IsProjectAdminAsync(task.ProjectId, userId))
        {
            throw new UnauthorizedAccessException("Only project admins can delete tasks");
        }

        var taskKey = task.Key;
        var projectId = task.ProjectId;

        _unitOfWork.Tasks.Delete(task);
        await _unitOfWork.CompleteAsync();

        await _notificationService.NotifyTaskDeletedAsync(projectId, taskKey);
    }

    public async Task<BoardDto> GetBoardAsync(string projectId, string userId)
    {
        if (!await _projectService.IsProjectMemberAsync(projectId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this project");
        }

        var tasks = await _unitOfWork.Tasks.Query()
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var board = new BoardDto();
        foreach (TaskStatus status in Enum.GetValues(typeof(TaskStatus)))
        {
            board.Columns[status] = tasks
                .Where(t => t.Status == status)
                .Select(MapToTaskDto)
                .ToList();
        }

        return board;
    }

    private TaskDto MapToTaskDto(TaskProject task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Key = task.Key,
            Summary = task.Summary,
            Description = task.Description,
            Type = task.Type,
            Status = task.Status,
            Priority = task.Priority,
            ProjectId = task.ProjectId,
            AssigneeId = task.AssigneeId,
            Assignee = task.Assignee != null ? new UserDto
            {
                Id = task.Assignee.Id,
                Email = task.Assignee.Email ?? string.Empty,
                FirstName = task.Assignee.FirstName,
                LastName = task.Assignee.LastName,
                AvatarUrl = task.Assignee.AvatarUrl
            } : null,
            ReporterId = task.ReporterId,
            Reporter = new UserDto
            {
                Id = task.Reporter.Id,
                Email = task.Reporter.Email ?? string.Empty,
                FirstName = task.Reporter.FirstName,
                LastName = task.Reporter.LastName,
                AvatarUrl = task.Reporter.AvatarUrl
            },
            DueDate = task.DueDate,
            Labels = task.Labels != null ? JsonSerializer.Deserialize<List<string>>(task.Labels) : null,
            SprintId = task.SprintId,
            EpicId = task.EpicId,
            ParentId = task.ParentId,
            Attachments = task.Attachments != null ? JsonSerializer.Deserialize<List<string>>(task.Attachments) : null,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
