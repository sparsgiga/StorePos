using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Persistence.Sequences;

namespace StorePos.Persistence.Configurations;

public sealed class ManualProductCodeSequenceConfiguration
    : IEntityTypeConfiguration<ManualProductCodeSequence>
{
    public void Configure(EntityTypeBuilder<ManualProductCodeSequence> builder)
    {
        builder.ToTable("ManualProductCodeSequence", "dbo", table =>
        {
            table.HasCheckConstraint(
                "CK_ManualProductCodeSequence_Singleton",
                "[Id] = 1");
            table.HasCheckConstraint(
                "CK_ManualProductCodeSequence_NextCode",
                "[NextCode] >= 1000");
        });

        builder.HasKey(sequence => sequence.Id);
        builder.Property(sequence => sequence.Id).ValueGeneratedNever();
        builder.Property(sequence => sequence.NextCode).IsRequired();
        builder.Property(sequence => sequence.RowVersion).IsRowVersion();
    }
}
