using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Shared.Models;

public class TodoItem
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    
    public int OrderIndex { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public class TodoItemCreateDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
}

public class TodoItemUpdateDto
{
    [StringLength(500, MinimumLength = 1)]
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    public TodoStatus? Status { get; set; }
    
    public int? OrderIndex { get; set; }
}

public class TodoItemResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; }
    public int OrderIndex { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TodoReorderDto
{
    [Required]
    public Guid TodoId { get; set; }
    
    [Required]
    public int NewOrderIndex { get; set; }
    
    public TodoStatus? NewStatus { get; set; }
}