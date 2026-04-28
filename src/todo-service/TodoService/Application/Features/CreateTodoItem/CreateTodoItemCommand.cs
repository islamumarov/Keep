using MediatR;
using TodoService.Application.DTOs;
using TodoService.Application.Interfaces;
using TodoService.Domain.Entities;

namespace TodoService.Application.Features.CreateTodoItem;

public record CreateTodoItemCommand(
    string Title,
    string? Description,
    DateTime? DueDate,
    Guid OwnerId
) : IRequest<TodoItemDto>;


public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, TodoItemDto>
{
    private readonly ITodoRepository _repository;

    public CreateTodoItemCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoItemDto> Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        var item = TodoItem.Create(
            request.Title,
            request.Description,
            request.DueDate,
            request.OwnerId);

        await _repository.AddAsync(item, ct);
        
        return new TodoItemDto(
            item.Id, item.Title, item.Description, item.DueDate,
            item.IsCompleted, item.OwnerId, item.CreatedAt, item.UpdatedAt);
    }
}