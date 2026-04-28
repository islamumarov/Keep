using TodoService.Application.DTOs;
using TodoService.Domain.Entities;

namespace TodoService.Application.Mappings;

public static class TodoMapping
{
    public static TodoItemDto ToDto(this TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.DueDate,
        item.IsCompleted,
        item.OwnerId,
        item.CreatedAt,
        item.UpdatedAt
    );

    public static TodoItemSummaryDto ToSummaryDto(this TodoItem item) => new(
        item.Id,
        item.Title,
        item.DueDate,
        item.IsCompleted
    );

    public static void ApplyUpdate(this TodoItem item, UpdateTodoItemRequest req)
    {
        item.Update(
            title: req.Title,
            description: req.Description,
            dueDate: req.DueDate
        );

        if (req.IsCompleted.HasValue)
        {
            if (req.IsCompleted.Value)
                item.MarkAsCompleted();
            else
                item.Reopen();
        }
    }
}