using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Persistence.Configurations;

public sealed class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PaymentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PaymentKind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CompletionVersion).IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.OperationId);

        builder.Property(x => x.DateCreated).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DateUpdated).HasColumnType("datetime2");

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => new { x.SaleId, x.CompletionVersion });
        builder.HasIndex(x => x.OperationId)
            .IsUnique()
            .HasFilter("[OperationId] IS NOT NULL");
    }
}
