using System;
using TodoService.Domain.Common;
using TodoService.Domain.Exceptions;
using TodoService.Domain.ValueObjects;

namespace TodoService.Domain.Entities;

public class TodoItem : Entity
{
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid OwnerId { get; private set; }      
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Protected constructor for EF Core / deserialization
    protected TodoItem() { }

    private TodoItem(
        string title,
        string? description,
        DateTime? dueDate,
        Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new DomainException("OwnerId is required.");

        Title = TodoTitle.Create(title);
        Description = description?.Trim();
        DueDate = dueDate;
        OwnerId = ownerId;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }

    public static TodoItem Create(
        string title,
        string? description,
        DateTime? dueDate,
        Guid ownerId)
    {
        return new TodoItem(title, description, dueDate, ownerId);
    }

    public void Update(
        string? title = null,
        string? description = null,
        DateTime? dueDate = null)
    {
        if (title is not null)
        {
            // Title = TodoTitle.Create(title);   // if using value object
            Title = title.Trim();
            if (string.IsNullOrWhiteSpace(Title))
                throw new DomainException("Title cannot be empty.");
        }

        if (description is not null)
            Description = description.Trim();

        DueDate = dueDate;

        UpdatedAt = DateTime.UtcNow;

        // Optional: AddDomainEvent(new TodoItemUpdatedEvent(Id));
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted)
            return; // or throw if you want strict rules

        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;

        // Optional: AddDomainEvent(new TodoItemCompletedEvent(Id));
    }

    public void Reopen()
    {
        if (!IsCompleted)
            return;

        IsCompleted = false;
        UpdatedAt = DateTime.UtcNow;
    }
}