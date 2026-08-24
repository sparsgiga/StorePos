using StorePos.Domain.Base;

namespace StorePos.Domain.Interfaces;

public interface IQueryRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    IQueryable<TEntity> Query();
}
