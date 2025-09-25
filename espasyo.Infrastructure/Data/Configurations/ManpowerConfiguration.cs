using espasyo.Domain.Entities;
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

        builder.Property(m => m.PrecinctId)
            .IsRequired();

        builder.Property(m => m.Shift)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.HeadCount)
            .IsRequired();

        builder.Property(m => m.LastUpdated)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        // Configure relationship with Precinct
        builder.HasOne(m => m.Precinct)
            .WithMany(p => p.ManpowerAllocations)
            .HasForeignKey(m => m.PrecinctId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Manpower_Precinct");

        // Create unique index on PrecinctId and Shift to ensure one manpower record per precinct per shift
        builder.HasIndex(m => new { m.PrecinctId, m.Shift })
            .IsUnique()
            .HasDatabaseName("IX_Manpower_PrecinctId_Shift");
    }
}
