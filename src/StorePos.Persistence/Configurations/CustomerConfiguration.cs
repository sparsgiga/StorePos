using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.Customer;

namespace StorePos.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedOnAdd();

        builder.Property(customer => customer.Name)
            .HasMaxLength(Customer.NameMaxLength)
            .IsRequired();
        builder.Property(customer => customer.IdentificationNumber)
            .HasMaxLength(Customer.IdentificationNumberMaxLength);
        builder.Property(customer => customer.Information)
            .HasMaxLength(Customer.InformationMaxLength);
        builder.Property(customer => customer.DateCreated)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(customer => customer.DateUpdated)
            .HasColumnType("datetime2");

        builder.HasIndex(customer => customer.Name);
        builder.HasIndex(customer => customer.IdentificationNumber)
            .IsUnique()
            .HasFilter("[IdentificationNumber] IS NOT NULL");
    }
}
