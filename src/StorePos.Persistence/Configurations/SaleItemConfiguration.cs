using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.MeasurementUnit;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Persistence.Configurations;

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ProductCode)
            .HasMaxLength(50);

        builder.Property(x => x.Barcode)
            .HasMaxLength(100);

        builder.Property(x => x.ProductName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.MeasurementUnitId)
            .HasColumnName("UnitId");

        builder.Property(x => x.MeasurementUnitName)
            .HasColumnName("UnitName")
            .HasMaxLength(100);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 5)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 5)
            .IsRequired();

        builder.Property(x => x.LineTotal)
            .HasPrecision(18, 5)
            .IsRequired();

        builder.Property(x => x.IsManual).IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(500);

        builder.Property(x => x.DateCreated).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DateUpdated).HasColumnType("datetime2");

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => x.ProductId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MeasurementUnit>()
            .WithMany()
            .HasForeignKey(x => x.MeasurementUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
