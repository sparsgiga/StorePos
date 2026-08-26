using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Enums;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class SaleRepository(StorePosDbContext context)
    : Repository<Sale, long>(context), ISaleRepository
{
    public async Task<IReadOnlyList<Sale>> GetDraftsAsync(
        CancellationToken cancellationToken = default)
        => await Entities
            .AsNoTracking()
            .Where(sale => sale.Status == SaleStatus.Draft)
            .OrderBy(sale => sale.DateCreated)
            .ThenBy(sale => sale.Id)
            .ToListAsync(cancellationToken);

    public Task<Sale?> GetDraftForUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default)
        => Entities
            .Include(sale => sale.Items)
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(
                sale => sale.Id == saleId && sale.Status == SaleStatus.Draft,
                cancellationToken);

    public Task<Sale?> GetDraftForMetadataUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default)
        => Entities.SingleOrDefaultAsync(
            sale => sale.Id == saleId && sale.Status == SaleStatus.Draft,
            cancellationToken);

    public Task<Sale?> GetDraftDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .Include(sale => sale.Items)
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(
                sale => sale.Id == saleId && sale.Status == SaleStatus.Draft,
                cancellationToken);

    public Task<Sale?> GetCompletedForUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default)
        => Entities
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(
                sale => sale.Id == saleId && sale.Status == SaleStatus.Completed,
                cancellationToken);

    public Task<Sale?> GetByDebtPaymentOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
        => Entities
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(
                sale => sale.Payments.Any(payment => payment.OperationId == operationId),
                cancellationToken);
}
