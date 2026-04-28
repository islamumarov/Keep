using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoService.Domain.Entities;

namespace TodoService.Infrastructure.Persistence.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.OwnerId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
           ;// .HasDefaultValueSql("GETUTCDATE()");

        // Index for fast owner-based queries
        builder.HasIndex(e => e.OwnerId);
    }
}