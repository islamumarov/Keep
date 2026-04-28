using MediatR;
using TodoService.Application.DTOs;
using TodoService.Application.Interfaces;

namespace TodoService.Application.Features.GetTodoItems;

public class GetTodoItemsQueryHandler : IRequestHandler<GetTodoItemsQuery, IReadOnlyList<TodoItemSummaryDto>>
{
    private readonly ITodoRepository _repository;

    public GetTodoItemsQueryHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TodoItemSummaryDto>> Handle(GetTodoItemsQuery request, CancellationToken ct)
    {
        var items = await _repository.GetAllByOwnerAsync(request.OwnerId, ct);

        return items.Select(i => new TodoItemSummaryDto(
            i.Id,
            i.Title,
            i.DueDate,
            i.IsCompleted
        )).ToList();
    }
}
