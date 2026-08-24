using StorePos.Domain.Base;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public class Repository<TEntity, TId>(StorePosDbContext context)
    : QueryRepository<TEntity, TId>(context), IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
        => await Entities.FindAsync([id], cancellationToken);

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
        => await Entities.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => Entities.Update(entity);

    public void Remove(TEntity entity) => Entities.Remove(entity);
}
