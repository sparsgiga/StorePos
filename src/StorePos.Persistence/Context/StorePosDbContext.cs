using Microsoft.EntityFrameworkCore;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Aggregates.User;
using StorePos.Domain.Interfaces;
using StorePos.Persistence.Sequences;

namespace StorePos.Persistence.Context;

public sealed class StorePosDbContext(DbContextOptions<StorePosDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();

    public DbSet<ManualProductCodeSequence> ManualProductCodeSequences =>
        Set<ManualProductCodeSequence>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    public DbSet<User> Users => Set<User>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorePosDbContext).Assembly);
    }

    private void ApplyAuditValues()
    {
        var localNow = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries<IAudit>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DateCreated = localNow;
                entry.Entity.DateUpdated = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAudit.DateCreated)).IsModified = false;
                entry.Entity.DateUpdated = localNow;
            }
        }
    }
}
