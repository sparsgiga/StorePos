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

        var seedDate = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            CreateSeed(1, "ც", "ცალი", seedDate),
            CreateSeed(2, "კგ", "კილოგრამი", seedDate),
            CreateSeed(3, "მ", "მეტრი", seedDate),
            CreateSeed(4, "ლ", "ლიტრი", seedDate),
            CreateSeed(5, "გრ", "გრამი", seedDate),
            CreateSeed(6, "ტ", "ტონა", seedDate),
            CreateSeed(7, "წყ", "წყვილი (2 ცალი)", seedDate),
            CreateSeed(8, "სმ", "სანტიმეტრი", seedDate),
            CreateSeed(9, "კვ.მ", "კვადრატული მეტრი", seedDate),
            CreateSeed(10, "ყთ", "ყუთი", seedDate),
            CreateSeed(11, "ქ", "ქილა", seedDate),
            CreateSeed(12, "ბთ", "ბოთლი", seedDate),
            CreateSeed(13, "კმ", "კილომეტრი", seedDate),
            CreateSeed(14, "კვ.სმ", "კვადრატული სანტიმეტრი", seedDate),
            CreateSeed(15, "კბ.მ", "კუბური მეტრი", seedDate),
            CreateSeed(16, "მმ", "მილილიტრი", seedDate),
            CreateSeed(17, "სხვ", "სხვა", seedDate),
            CreateSeed(18, "ჭიქა", "ჭიქა", seedDate),
            CreateSeed(19, "კომპლექტი", "კომპლექტი", seedDate),
            CreateSeed(20, "რულონი", "რულონი", seedDate),
            CreateSeed(21, "ტომარა", "ტომარა", seedDate),
            CreateSeed(22, "შეკვრა", "შეკვრა", seedDate),
            CreateSeed(24, "მ³", "მ³", seedDate));
    }

    private static object CreateSeed(
        int id,
        string shortName,
        string name,
        DateTime dateCreated)
        => new
        {
            Id = id,
            Name = name,
            ShortName = shortName,
            Code = (string?)null,
            IsActive = true,
            DateCreated = dateCreated,
            DateUpdated = (DateTime?)null
        };
}
