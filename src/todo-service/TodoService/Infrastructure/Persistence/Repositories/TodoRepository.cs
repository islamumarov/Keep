using Microsoft.EntityFrameworkCore;
using TodoService.Application.Interfaces;
using TodoService.Domain.Entities;
namespace TodoService.Infrastructure.Persistence.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _context;

    public TodoRepository(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _context.TodoItems
            .Where(t => t.OwnerId == ownerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(TodoItem item, CancellationToken ct = default)
    {
        await _context.TodoItems.AddAsync(item, ct);
    }

    public void Update(TodoItem item)
    {
        // EF tracks changes automatically if attached
        // or call _context.Update(item) if detached
    }

    public void Remove(TodoItem item)
    {
        _context.TodoItems.Remove(item);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TodoItems.AnyAsync(t => t.Id == id, ct);
    }
}
