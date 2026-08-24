using StorePos.Domain.Base;

namespace StorePos.Domain.Interfaces;

public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
