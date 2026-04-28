using MediatR;
using Microsoft.EntityFrameworkCore;

using TodoService.Infrastructure.Persistence;

namespace TodoService.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly TodoDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        TodoDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Option A: Apply transaction to *all* requests (simple but overkill for queries)
        // return await ExecuteWithTransactionAsync(next, cancellationToken);

        // Option B: Apply only to commands (recommended – most common pattern 2025+)
        // Heuristic: if request name ends with "Command" (convention-based)
        var requestName = request.GetType().Name;
        if (requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteWithTransactionAsync(next, cancellationToken);
        }

        // Queries → just proceed without transaction
        return await next();
    }

    private async Task<TResponse> ExecuteWithTransactionAsync(
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                _logger.LogInformation("Transaction started for {RequestName}", typeof(TRequest).Name);

                var response = await next();

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Transaction committed for {RequestName}", typeof(TRequest).Name);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Transaction rolled back for {RequestName}", typeof(TRequest).Name);

                throw;  // rethrow → global exception handling or caller handles
            }
        });
    }
}