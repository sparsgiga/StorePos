using Microsoft.EntityFrameworkCore.Storage;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;

namespace StorePos.Persistence;

public sealed class UnitOfWork(StorePosDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = GetActiveTransaction();
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = GetActiveTransaction();
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public ValueTask DisposeAsync() => DisposeTransactionAsync();

    private IDbContextTransaction GetActiveTransaction()
        => _transaction ?? throw new InvalidOperationException("No transaction is active.");

    private async ValueTask DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
