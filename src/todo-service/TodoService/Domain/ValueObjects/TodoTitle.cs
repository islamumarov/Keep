// Domain/ValueObjects/TodoTitle.cs
using System;
using TodoService.Domain.Exceptions;

namespace TodoService.Domain.ValueObjects;

public sealed record TodoTitle
{
    public string Value { get; }

    private TodoTitle(string value) => Value = value;

    public static TodoTitle Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("Title cannot be empty.");

        var trimmed = input.Trim();

        if (trimmed.Length < 3)
            throw new DomainException("Title must be at least 3 characters.");

        if (trimmed.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters.");

        return new TodoTitle(trimmed);
    }

    public static implicit operator string(TodoTitle title) => title.Value;
    public static implicit operator TodoTitle(string value) => Create(value);
}
