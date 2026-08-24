using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Base;

public abstract class AuditableEntity<TId> : Entity<TId>, IAudit
    where TId : notnull
{
    public DateTime DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }
}
