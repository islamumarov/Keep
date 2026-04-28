using MediatR;
using TodoService.Application.Interfaces;
using TodoService.Domain.Exceptions;

namespace TodoService.Application.Features.DeleteTodoItem;

public record DeleteTodoItemCommand(
    Guid Id,
    Guid OwnerId
) : IRequest<bool>;



public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, bool>
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<DeleteTodoItemCommandHandler> _logger;

    public DeleteTodoItemCommandHandler(
        ITodoRepository repository,
        ILogger<DeleteTodoItemCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteTodoItemCommand request, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(request.Id, ct);

        if (item == null)
        {
            _logger.LogInformation("Attempt to delete non-existing todo {Id}", request.Id);
            return false;
        }

        // Ownership check
        if (item.OwnerId != request.OwnerId)
        {
            _logger.LogWarning("Unauthorized delete attempt on todo {TodoId} by user {UserId}",
                request.Id, request.OwnerId);
            throw new DomainException("You do not have permission to delete this item.");
        }

        _repository.Remove(item);

        // TransactionBehavior handles SaveChanges + commit/rollback

        _logger.LogInformation("Todo item {Id} deleted by owner {OwnerId}", request.Id, request.OwnerId);
        return true;
    }
}
