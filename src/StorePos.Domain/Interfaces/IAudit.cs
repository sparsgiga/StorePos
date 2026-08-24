namespace StorePos.Domain.Interfaces;

public interface IAudit
{
    DateTime DateCreated { get; set; }

    DateTime? DateUpdated { get; set; }
}
