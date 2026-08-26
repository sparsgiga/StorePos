using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.Customer;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Aggregates.User;

namespace StorePos.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SaleNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CustomerName)
            .HasMaxLength(Sale.CustomerNameMaxLength);

        builder.Property(x => x.CustomerIdentificationNumber)
            .HasMaxLength(Sale.CustomerIdentificationNumberMaxLength);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.FinancialRevision).IsRequired();
        builder.Property(x => x.CompletionVersion).IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(Sale.CommentMaxLength);

        builder.Property(x => x.DateCreated).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DateUpdated).HasColumnType("datetime2");
        builder.Property(x => x.DateCompleted).HasColumnType("datetime2");
        builder.Property(x => x.DateCancelled).HasColumnType("datetime2");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.SaleNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DateCreated);
        builder.HasIndex(x => x.CustomerId);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
