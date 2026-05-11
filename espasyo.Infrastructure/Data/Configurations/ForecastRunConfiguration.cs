using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class ForecastRunConfiguration : IEntityTypeConfiguration<ForecastRun>
{
    public void Configure(EntityTypeBuilder<ForecastRun> builder)
    {
        builder.ToTable("ForecastRun");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(f => f.PrecinctId).IsRequired();
        builder.Property(f => f.RunAt).IsRequired();
        builder.Property(f => f.Horizon).IsRequired();
        builder.Property(f => f.ConfidenceLevel).IsRequired().HasColumnType("float");
        builder.Property(f => f.ModelType).IsRequired().HasConversion<int>();
        builder.Property(f => f.Status).IsRequired().HasConversion<int>();
        builder.Property(f => f.TotalSeries).IsRequired().HasDefaultValue(0);
        builder.Property(f => f.GeneratedById).IsRequired().HasMaxLength(450);

        builder.HasOne(f => f.Precinct)
            .WithMany()
            .HasForeignKey(f => f.PrecinctId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ForecastRun_Precinct");

        builder.HasIndex(f => f.RunAt).HasDatabaseName("IX_ForecastRun_RunAt");
        builder.HasIndex(f => f.Status).HasDatabaseName("IX_ForecastRun_Status");
    }
}
