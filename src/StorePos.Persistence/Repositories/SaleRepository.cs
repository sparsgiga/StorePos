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
}
