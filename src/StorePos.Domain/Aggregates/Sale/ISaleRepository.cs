using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.Sale;

public interface ISaleRepository :
    IRepository<Sale, long>,
    IQueryRepository<Sale, long>
{
    Task<IReadOnlyList<Sale>> GetDraftsAsync(
        CancellationToken cancellationToken = default);

    Task<Sale?> GetDraftForUpdateAsync(
        long saleId,
        CancellationToken cancellationToken = default);

    Task<Sale?> GetDraftDetailsAsync(
        long saleId,
        CancellationToken cancellationToken = default);
}
