using TodoService.Domain.Entities;

namespace TodoService.Application.Interfaces;

public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TodoItem>> GetAllByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    Task AddAsync(TodoItem item, CancellationToken ct = default);

    void Update(TodoItem item);           // usually tracked → no async needed

    void Remove(TodoItem item);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}