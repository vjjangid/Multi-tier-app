using KanbanBoard.Common.Responses;
using KanbanBoard.Shared.Models;
using KanbanService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KanbanController : ControllerBase
{
    private readonly IKanbanService _kanbanService;
    private readonly ILogger<KanbanController> _logger;

    public KanbanController(IKanbanService kanbanService, ILogger<KanbanController> logger)
    {
        _kanbanService = kanbanService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid user ID in token");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<KanbanTaskDto>>>> GetTasks()
    {
        try
        {
            var userId = GetCurrentUserId();
            var tasks = await _kanbanService.GetTasksByUserIdAsync(userId);
            
            return Ok(ApiResponse<IEnumerable<KanbanTaskDto>>.SuccessResponse(tasks, "Tasks retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<IEnumerable<KanbanTaskDto>>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks");
            return StatusCode(500, ApiResponse<IEnumerable<KanbanTaskDto>>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<KanbanTaskDto>>> GetTask(Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var task = await _kanbanService.GetTaskByIdAsync(taskId, userId);
            
            if (task == null)
            {
                return NotFound(ApiResponse<KanbanTaskDto>.ErrorResponse("Task not found", statusCode: 404));
            }

            return Ok(ApiResponse<KanbanTaskDto>.SuccessResponse(task, "Task retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<KanbanTaskDto>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task: {TaskId}", taskId);
            return StatusCode(500, ApiResponse<KanbanTaskDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<KanbanTaskDto>>> CreateTask([FromBody] CreateKanbanTaskDto createTaskDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<KanbanTaskDto>.ErrorResponse("Validation failed", errors, 400));
            }

            var userId = GetCurrentUserId();
            var task = await _kanbanService.CreateTaskAsync(createTaskDto, userId);

            return CreatedAtAction(nameof(GetTask), new { taskId = task.Id }, 
                ApiResponse<KanbanTaskDto>.SuccessResponse(task, "Task created successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<KanbanTaskDto>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, ApiResponse<KanbanTaskDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<KanbanTaskDto>>> UpdateTask(Guid taskId, [FromBody] UpdateKanbanTaskDto updateTaskDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<KanbanTaskDto>.ErrorResponse("Validation failed", errors, 400));
            }

            var userId = GetCurrentUserId();
            var task = await _kanbanService.UpdateTaskAsync(taskId, updateTaskDto, userId);

            if (task == null)
            {
                return NotFound(ApiResponse<KanbanTaskDto>.ErrorResponse("Task not found", statusCode: 404));
            }

            return Ok(ApiResponse<KanbanTaskDto>.SuccessResponse(task, "Task updated successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<KanbanTaskDto>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task: {TaskId}", taskId);
            return StatusCode(500, ApiResponse<KanbanTaskDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTask(Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _kanbanService.DeleteTaskAsync(taskId, userId);

            if (!success)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Task not found", statusCode: 404));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Task deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task: {TaskId}", taskId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPost("{taskId:guid}/move")]
    public async Task<ActionResult<ApiResponse<KanbanTaskDto>>> MoveTask(Guid taskId, [FromBody] MoveKanbanTaskDto moveTaskDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<KanbanTaskDto>.ErrorResponse("Validation failed", errors, 400));
            }

            var userId = GetCurrentUserId();
            var task = await _kanbanService.MoveTaskAsync(taskId, moveTaskDto, userId);

            if (task == null)
            {
                return NotFound(ApiResponse<KanbanTaskDto>.ErrorResponse("Task not found", statusCode: 404));
            }

            return Ok(ApiResponse<KanbanTaskDto>.SuccessResponse(task, "Task moved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<KanbanTaskDto>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving task: {TaskId}", taskId);
            return StatusCode(500, ApiResponse<KanbanTaskDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResponse<IEnumerable<KanbanTaskDto>>>> ReorderTasks([FromBody] ReorderTasksDto reorderDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<IEnumerable<KanbanTaskDto>>.ErrorResponse("Validation failed", errors, 400));
            }

            var userId = GetCurrentUserId();
            var tasks = await _kanbanService.ReorderTasksAsync(userId, reorderDto.Status, reorderDto.TaskIds);

            return Ok(ApiResponse<IEnumerable<KanbanTaskDto>>.SuccessResponse(tasks, "Tasks reordered successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<IEnumerable<KanbanTaskDto>>.ErrorResponse(ex.Message, statusCode: 401));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering tasks");
            return StatusCode(500, ApiResponse<IEnumerable<KanbanTaskDto>>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }
}

public class ReorderTasksDto
{
    public TodoStatus Status { get; set; }
    public List<Guid> TaskIds { get; set; } = new();
}