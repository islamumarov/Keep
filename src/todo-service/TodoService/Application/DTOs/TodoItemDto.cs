using System.ComponentModel.DataAnnotations;

namespace TodoService.Application.DTOs;

public record TodoItemDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime? DueDate,
    bool IsCompleted,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
public record TodoItemSummaryDto(
    Guid Id,
    string Title,
    DateTime? DueDate,
    bool IsCompleted
);
public record CreateTodoItemRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = null!;

    [StringLength(2000)]
    public string? Description { get; init; }

    public DateTime? DueDate { get; init; }

    // OwnerId is NOT here — taken from JWT claim (sub / userId)
};

public record UpdateTodoItemRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? DueDate { get; init; }
    public bool? IsCompleted { get; init; }   // optional partial update
};