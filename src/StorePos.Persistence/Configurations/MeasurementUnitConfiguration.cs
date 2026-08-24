using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.MeasurementUnit;

namespace StorePos.Persistence.Configurations;

public sealed class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("Units", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ShortName)
            .HasMaxLength(20);

        builder.Property(x => x.Code)
            .HasMaxLength(20);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DateCreated).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DateUpdated).HasColumnType("datetime2");
    }
}
