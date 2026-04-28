using MediatR;
using TodoService.Application.DTOs;
using TodoService.Application.Interfaces;
using TodoService.Domain.Exceptions;

namespace TodoService.Application.Features.UpdateTodoItem;

public record UpdateTodoItemCommand(
    Guid Id,
    Guid OwnerId,                     // for ownership check
    UpdateTodoItemRequest Request
) : IRequest<TodoItemDto?>;





public class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, TodoItemDto?>
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<UpdateTodoItemCommandHandler> _logger;

    public UpdateTodoItemCommandHandler(
        ITodoRepository repository,
        ILogger<UpdateTodoItemCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TodoItemDto?> Handle(UpdateTodoItemCommand request, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(request.Id, ct);

        if (item == null)
        {
            _logger.LogWarning("Todo item {Id} not found", request.Id);
            return null;
        }

        // Ownership check – very important security boundary
        if (item.OwnerId != request.OwnerId)
        {
            _logger.LogWarning("User {UserId} attempted to update foreign todo {TodoId}",
                request.OwnerId, request.Id);
            throw new DomainException("You do not have permission to update this item.");
        }

        // Apply changes – domain method encapsulates rules
        item.Update(
            title: request.Request.Title,
            description: request.Request.Description,
            dueDate: request.Request.DueDate
        );

        if (request.Request.IsCompleted.HasValue)
        {
            if (request.Request.IsCompleted.Value)
                item.MarkAsCompleted();
            else
                item.Reopen();
        }

        _repository.Update(item);

        // TransactionBehavior will call SaveChangesAsync() and commit
        // No explicit SaveChanges here

        return new TodoItemDto(
            item.Id,
            item.Title,
            item.Description,
            item.DueDate,
            item.IsCompleted,
            item.OwnerId,
            item.CreatedAt,
            item.UpdatedAt
        );
    }
}