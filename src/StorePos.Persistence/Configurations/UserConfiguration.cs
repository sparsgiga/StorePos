using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StorePos.Domain.Aggregates.User;

namespace StorePos.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DateCreated).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DateUpdated).HasColumnType("datetime2");

        builder.HasIndex(x => x.Username).IsUnique();
    }
}
