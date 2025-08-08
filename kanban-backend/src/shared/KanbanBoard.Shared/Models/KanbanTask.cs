namespace KanbanBoard.Shared.Models;

public class KanbanTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public int Order { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}

public enum TodoStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

// DTOs for API
public class KanbanTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; }
    public int Order { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateKanbanTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
}

public class UpdateKanbanTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TodoStatus? Status { get; set; }
    public int? Order { get; set; }
}

public class MoveKanbanTaskDto
{
    public TodoStatus NewStatus { get; set; }
    public int NewOrder { get; set; }
}