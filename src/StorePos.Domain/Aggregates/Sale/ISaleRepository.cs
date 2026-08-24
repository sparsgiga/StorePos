using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.Sale;

public interface ISaleRepository :
    IRepository<Sale, long>,
    IQueryRepository<Sale, long>
{
    Task<IReadOnlyList<Sale>> GetDraftsAsync(
        CancellationToken cancellationToken = default);
}
