using KanbanBoard.Shared.Models;
using KanbanService.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanService.Services;

public interface IKanbanService
{
    Task<IEnumerable<KanbanTaskDto>> GetTasksByUserIdAsync(Guid userId);
    Task<KanbanTaskDto?> GetTaskByIdAsync(Guid taskId, Guid userId);
    Task<KanbanTaskDto> CreateTaskAsync(CreateKanbanTaskDto createTaskDto, Guid userId);
    Task<KanbanTaskDto?> UpdateTaskAsync(Guid taskId, UpdateKanbanTaskDto updateTaskDto, Guid userId);
    Task<bool> DeleteTaskAsync(Guid taskId, Guid userId);
    Task<KanbanTaskDto?> MoveTaskAsync(Guid taskId, MoveKanbanTaskDto moveTaskDto, Guid userId);
    Task<IEnumerable<KanbanTaskDto>> ReorderTasksAsync(Guid userId, TodoStatus status, List<Guid> taskIds);
}

public class KanbanTaskService : IKanbanService
{
    private readonly KanbanDbContext _context;
    private readonly ILogger<KanbanTaskService> _logger;

    public KanbanTaskService(KanbanDbContext context, ILogger<KanbanTaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<KanbanTaskDto>> GetTasksByUserIdAsync(Guid userId)
    {
        try
        {
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.Order)
                .ToListAsync();

            return tasks.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<KanbanTaskDto?> GetTaskByIdAsync(Guid taskId, Guid userId)
    {
        try
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            return task != null ? MapToDto(task) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task: {TaskId} for user: {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task<KanbanTaskDto> CreateTaskAsync(CreateKanbanTaskDto createTaskDto, Guid userId)
    {
        try
        {
            // Get the next order number for the status
            var maxOrder = await _context.Tasks
                .Where(t => t.UserId == userId && t.Status == createTaskDto.Status)
                .MaxAsync(t => (int?)t.Order) ?? 0;

            var task = new KanbanTask
            {
                Id = Guid.NewGuid(),
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                Status = createTaskDto.Status,
                Order = maxOrder + 1,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task created: {TaskId} for user: {UserId}", task.Id, userId);
            return MapToDto(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<KanbanTaskDto?> UpdateTaskAsync(Guid taskId, UpdateKanbanTaskDto updateTaskDto, Guid userId)
    {
        try
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(updateTaskDto.Title))
                task.Title = updateTaskDto.Title;

            if (!string.IsNullOrEmpty(updateTaskDto.Description))
                task.Description = updateTaskDto.Description;

            if (updateTaskDto.Status.HasValue && updateTaskDto.Status != task.Status)
            {
                // Handle status change - reorder tasks
                await HandleStatusChangeAsync(task, updateTaskDto.Status.Value, updateTaskDto.Order);
            }
            else if (updateTaskDto.Order.HasValue && updateTaskDto.Order != task.Order)
            {
                // Handle order change within same status
                await HandleOrderChangeAsync(task, updateTaskDto.Order.Value);
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task updated: {TaskId} for user: {UserId}", taskId, userId);
            return MapToDto(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task: {TaskId} for user: {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid userId)
    {
        try
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                return false;
            }

            _context.Tasks.Remove(task);

            // Reorder remaining tasks in the same status
            var tasksToReorder = await _context.Tasks
                .Where(t => t.UserId == userId && t.Status == task.Status && t.Order > task.Order)
                .OrderBy(t => t.Order)
                .ToListAsync();

            foreach (var taskToReorder in tasksToReorder)
            {
                taskToReorder.Order--;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task deleted: {TaskId} for user: {UserId}", taskId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task: {TaskId} for user: {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task<KanbanTaskDto?> MoveTaskAsync(Guid taskId, MoveKanbanTaskDto moveTaskDto, Guid userId)
    {
        try
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
            {
                return null;
            }

            await HandleStatusChangeAsync(task, moveTaskDto.NewStatus, moveTaskDto.NewOrder);
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task moved: {TaskId} to {NewStatus} for user: {UserId}", taskId, moveTaskDto.NewStatus, userId);
            return MapToDto(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving task: {TaskId} for user: {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<KanbanTaskDto>> ReorderTasksAsync(Guid userId, TodoStatus status, List<Guid> taskIds)
    {
        try
        {
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && t.Status == status && taskIds.Contains(t.Id))
                .ToListAsync();

            for (int i = 0; i < taskIds.Count; i++)
            {
                var task = tasks.FirstOrDefault(t => t.Id == taskIds[i]);
                if (task != null)
                {
                    task.Order = i + 1;
                    task.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tasks reordered for user: {UserId} in status: {Status}", userId, status);
            return tasks.OrderBy(t => t.Order).Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering tasks for user: {UserId}", userId);
            throw;
        }
    }

    private async Task HandleStatusChangeAsync(KanbanTask task, TodoStatus newStatus, int? newOrder = null)
    {
        var oldStatus = task.Status;
        var oldOrder = task.Order;

        // Get max order in new status
        var maxOrderInNewStatus = await _context.Tasks
            .Where(t => t.UserId == task.UserId && t.Status == newStatus && t.Id != task.Id)
            .MaxAsync(t => (int?)t.Order) ?? 0;

        var targetOrder = newOrder ?? maxOrderInNewStatus + 1;

        // Update tasks in new status (shift down)
        var tasksInNewStatus = await _context.Tasks
            .Where(t => t.UserId == task.UserId && t.Status == newStatus && t.Order >= targetOrder && t.Id != task.Id)
            .ToListAsync();

        foreach (var taskInNewStatus in tasksInNewStatus)
        {
            taskInNewStatus.Order++;
        }

        // Update tasks in old status (shift up)
        var tasksInOldStatus = await _context.Tasks
            .Where(t => t.UserId == task.UserId && t.Status == oldStatus && t.Order > oldOrder && t.Id != task.Id)
            .ToListAsync();

        foreach (var taskInOldStatus in tasksInOldStatus)
        {
            taskInOldStatus.Order--;
        }

        task.Status = newStatus;
        task.Order = targetOrder;
    }

    private async Task HandleOrderChangeAsync(KanbanTask task, int newOrder)
    {
        var currentOrder = task.Order;
        var tasksToUpdate = new List<KanbanTask>();

        if (newOrder > currentOrder)
        {
            // Moving down - shift tasks up
            tasksToUpdate = await _context.Tasks
                .Where(t => t.UserId == task.UserId && t.Status == task.Status && 
                           t.Order > currentOrder && t.Order <= newOrder && t.Id != task.Id)
                .ToListAsync();

            foreach (var taskToUpdate in tasksToUpdate)
            {
                taskToUpdate.Order--;
            }
        }
        else if (newOrder < currentOrder)
        {
            // Moving up - shift tasks down
            tasksToUpdate = await _context.Tasks
                .Where(t => t.UserId == task.UserId && t.Status == task.Status && 
                           t.Order >= newOrder && t.Order < currentOrder && t.Id != task.Id)
                .ToListAsync();

            foreach (var taskToUpdate in tasksToUpdate)
            {
                taskToUpdate.Order++;
            }
        }

        task.Order = newOrder;
    }

    private static KanbanTaskDto MapToDto(KanbanTask task)
    {
        return new KanbanTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Order = task.Order,
            UserId = task.UserId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}