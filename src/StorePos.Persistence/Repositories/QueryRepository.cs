using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Base;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public class QueryRepository<TEntity, TId>(StorePosDbContext context)
    : IQueryRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected StorePosDbContext Context { get; } = context;

    protected DbSet<TEntity> Entities { get; } = context.Set<TEntity>();

    public IQueryable<TEntity> Query() => Entities.AsNoTracking();
}
