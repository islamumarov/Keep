using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TodoService.Domain.Entities;

namespace TodoService.Infrastructure.Persistence;

public class TodoDbContext : DbContext
{
    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    // Optional: override SaveChangesAsync to dispatch domain events
}