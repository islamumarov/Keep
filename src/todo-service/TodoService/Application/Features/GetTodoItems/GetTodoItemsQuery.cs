using MediatR;
using TodoService.Application.DTOs;

namespace TodoService.Application.Features.GetTodoItems;

public record GetTodoItemsQuery(Guid OwnerId) : IRequest<IReadOnlyList<TodoItemSummaryDto>>;