using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;

namespace StorePos.Persistence;

public sealed class UnitOfWork(StorePosDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConcurrencyException(
                "The persisted record was changed by another operation.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw TranslateUniqueConstraintViolation(exception);
        }
    }

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

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static Exception TranslateUniqueConstraintViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        var product = exception.Entries
            .Select(entry => entry.Entity)
            .OfType<Product>()
            .FirstOrDefault();

        if (message.Contains("IX_Products_Code", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductCodeConflictException(product?.Code ?? string.Empty);
        }

        if (message.Contains("IX_Products_Barcode", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductBarcodeConflictException(product?.Barcode ?? string.Empty);
        }

        if (message.Contains("IX_SalePayments_OperationId", StringComparison.OrdinalIgnoreCase))
        {
            return new SaleOperationConflictException(
                "გადახდის ოპერაცია უკვე დამუშავებულია. განაახლეთ გაყიდვის მონაცემები.",
                exception);
        }

        return exception;
    }
}
