using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class ManpowerConfiguration : IEntityTypeConfiguration<Manpower>
{
    public void Configure(EntityTypeBuilder<Manpower> builder)
    {
        builder.ToTable("Manpower");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Precinct)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Year)
            .IsRequired();

        builder.Property(m => m.AllocatedCount)
            .IsRequired();

        builder.Property(m => m.MildThreshold)
            .IsRequired();

        builder.Property(m => m.ModerateThreshold)
            .IsRequired();

        builder.Property(m => m.CriticalThreshold)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        // Create unique index on Precinct + Year combination
        builder.HasIndex(m => new { m.Precinct, m.Year })
            .IsUnique()
            .HasDatabaseName("IX_Manpower_Precinct_Year");

        // Create index on Year for faster year-based queries
        builder.HasIndex(m => m.Year)
            .HasDatabaseName("IX_Manpower_Year");

        // Create index on Precinct for faster precinct-based queries
        builder.HasIndex(m => m.Precinct)
            .HasDatabaseName("IX_Manpower_Precinct");
    }
}