using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KanbanBoard.Shared.Models;
using KanbanService.Data;
using KanbanService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KanbanService.Tests;

public class KanbanServiceTests : IDisposable
{
    private readonly KanbanDbContext _context;
    private readonly Mock<ILogger<KanbanTaskService>> _mockLogger;
    private readonly KanbanTaskService _kanbanService;
    private readonly Guid _testUserId;

    public KanbanServiceTests()
    {
        var options = new DbContextOptionsBuilder<KanbanDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KanbanDbContext(options);
        _mockLogger = new Mock<ILogger<KanbanTaskService>>();
        _kanbanService = new KanbanTaskService(_context, _mockLogger.Object);
        _testUserId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetTasksByUserIdAsync_ExistingTasks_ReturnsOrderedTasks()
    {
        var task1 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 1",
            Description = "Description 1",
            Status = TodoStatus.Todo,
            Order = 2,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var task2 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 2",
            Description = "Description 2",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var task3 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 3",
            Description = "Description 3",
            Status = TodoStatus.InProgress,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        var result = await _kanbanService.GetTasksByUserIdAsync(_testUserId);
        var resultList = result.ToList();

        Assert.Equal(3, resultList.Count);
        Assert.Equal(task2.Id, resultList[0].Id);
        Assert.Equal(task1.Id, resultList[1].Id);
        Assert.Equal(task3.Id, resultList[2].Id);
    }

    [Fact]
    public async Task GetTasksByUserIdAsync_NoTasks_ReturnsEmpty()
    {
        var result = await _kanbanService.GetTasksByUserIdAsync(_testUserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTasksByUserIdAsync_DifferentUser_ReturnsEmpty()
    {
        var otherUserId = Guid.NewGuid();
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Other User Task",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = otherUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _kanbanService.GetTasksByUserIdAsync(_testUserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ExistingTask_ReturnsTask()
    {
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _kanbanService.GetTaskByIdAsync(task.Id, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.Title, result.Title);
        Assert.Equal(task.Description, result.Description);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistentTask_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _kanbanService.GetTaskByIdAsync(nonExistentId, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_DifferentUser_ReturnsNull()
    {
        var otherUserId = Guid.NewGuid();
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Other User Task",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = otherUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _kanbanService.GetTaskByIdAsync(task.Id, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTaskAsync_ValidTask_ReturnsCreatedTask()
    {
        var createTaskDto = new CreateKanbanTaskDto
        {
            Title = "New Task",
            Description = "New Description",
            Status = TodoStatus.Todo
        };

        var result = await _kanbanService.CreateTaskAsync(createTaskDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(createTaskDto.Title, result.Title);
        Assert.Equal(createTaskDto.Description, result.Description);
        Assert.Equal(createTaskDto.Status, result.Status);
        Assert.Equal(1, result.Order);
        Assert.Equal(_testUserId, result.UserId);

        var taskInDb = await _context.Tasks.FindAsync(result.Id);
        Assert.NotNull(taskInDb);
        Assert.Equal(createTaskDto.Title, taskInDb.Title);
    }

    [Fact]
    public async Task CreateTaskAsync_MultipleTasksInSameStatus_OrdersCorrectly()
    {
        var existingTask = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Existing Task",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(existingTask);
        await _context.SaveChangesAsync();

        var createTaskDto = new CreateKanbanTaskDto
        {
            Title = "New Task",
            Description = "New Description",
            Status = TodoStatus.Todo
        };

        var result = await _kanbanService.CreateTaskAsync(createTaskDto, _testUserId);

        Assert.Equal(2, result.Order);
    }

    [Fact]
    public async Task UpdateTaskAsync_ExistingTask_UpdatesSuccessfully()
    {
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updateTaskDto = new UpdateKanbanTaskDto
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var result = await _kanbanService.UpdateTaskAsync(task.Id, updateTaskDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(updateTaskDto.Title, result.Title);
        Assert.Equal(updateTaskDto.Description, result.Description);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateTaskAsync_NonExistentTask_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();
        var updateTaskDto = new UpdateKanbanTaskDto
        {
            Title = "Updated Title"
        };

        var result = await _kanbanService.UpdateTaskAsync(nonExistentId, updateTaskDto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_DeletesSuccessfully()
    {
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task to Delete",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 2,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var taskToReorder = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task to Reorder",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 3,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(task, taskToReorder);
        await _context.SaveChangesAsync();

        var result = await _kanbanService.DeleteTaskAsync(task.Id, _testUserId);

        Assert.True(result);

        var deletedTask = await _context.Tasks.FindAsync(task.Id);
        Assert.Null(deletedTask);

        var reorderedTask = await _context.Tasks.FindAsync(taskToReorder.Id);
        Assert.NotNull(reorderedTask);
        Assert.Equal(2, reorderedTask.Order);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistentTask_ReturnsFalse()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _kanbanService.DeleteTaskAsync(nonExistentId, _testUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task MoveTaskAsync_ValidMove_MovesSuccessfully()
    {
        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task to Move",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var moveTaskDto = new MoveKanbanTaskDto
        {
            NewStatus = TodoStatus.InProgress,
            NewOrder = 1
        };

        var result = await _kanbanService.MoveTaskAsync(task.Id, moveTaskDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(moveTaskDto.NewStatus, result.Status);
        Assert.Equal(moveTaskDto.NewOrder, result.Order);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task MoveTaskAsync_NonExistentTask_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();
        var moveTaskDto = new MoveKanbanTaskDto
        {
            NewStatus = TodoStatus.InProgress,
            NewOrder = 1
        };

        var result = await _kanbanService.MoveTaskAsync(nonExistentId, moveTaskDto, _testUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReorderTasksAsync_ValidReorder_ReordersSuccessfully()
    {
        var task1 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 1",
            Description = "Description 1",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var task2 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 2",
            Description = "Description 2",
            Status = TodoStatus.Todo,
            Order = 2,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        var taskIds = new List<Guid> { task2.Id, task1.Id };

        var result = await _kanbanService.ReorderTasksAsync(_testUserId, TodoStatus.Todo, taskIds);
        var resultList = result.ToList();

        Assert.Equal(2, resultList.Count);
        Assert.Equal(task2.Id, resultList[0].Id);
        Assert.Equal(1, resultList[0].Order);
        Assert.Equal(task1.Id, resultList[1].Id);
        Assert.Equal(2, resultList[1].Order);
    }

    [Fact]
    public async Task ReorderTasksAsync_EmptyTaskIds_ReturnsEmpty()
    {
        var result = await _kanbanService.ReorderTasksAsync(_testUserId, TodoStatus.Todo, new List<Guid>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateTaskAsync_StatusChange_HandlesStatusChangeCorrectly()
    {
        var task1 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 1",
            Description = "Description",
            Status = TodoStatus.Todo,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var task2 = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = "Task 2",
            Description = "Description",
            Status = TodoStatus.InProgress,
            Order = 1,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        var updateTaskDto = new UpdateKanbanTaskDto
        {
            Status = TodoStatus.InProgress,
            Order = 1
        };

        var result = await _kanbanService.UpdateTaskAsync(task1.Id, updateTaskDto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal(TodoStatus.InProgress, result.Status);

        var updatedTask2 = await _context.Tasks.FindAsync(task2.Id);
        Assert.NotNull(updatedTask2);
        Assert.Equal(2, updatedTask2.Order);
    }
}