using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class SpatialForecastResultConfiguration : IEntityTypeConfiguration<SpatialForecastResult>
{
    public void Configure(EntityTypeBuilder<SpatialForecastResult> builder)
    {
        builder.ToTable("SpatialForecastResult");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(r => r.ForecastRunId).IsRequired();
        builder.Property(r => r.Precinct).IsRequired().HasConversion<int>();
        builder.Property(r => r.ClusterId).IsRequired();
        builder.Property(r => r.Latitude).HasColumnType("float");
        builder.Property(r => r.Longitude).HasColumnType("float");
        builder.Property(r => r.Month).IsRequired();
        builder.Property(r => r.Year).IsRequired();
        builder.Property(r => r.PredictedValue).IsRequired().HasColumnType("float");
        builder.Property(r => r.LowerBound).IsRequired().HasColumnType("float");
        builder.Property(r => r.UpperBound).IsRequired().HasColumnType("float");
        builder.Property(r => r.Confidence).IsRequired().HasColumnType("float");
        builder.Property(r => r.RiskLevel).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Trend).IsRequired().HasMaxLength(20);

        builder.HasOne(r => r.ForecastRun)
            .WithMany()
            .HasForeignKey(r => r.ForecastRunId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SpatialForecastResult_ForecastRun");

        builder.HasIndex(r => r.ForecastRunId).HasDatabaseName("IX_SpatialForecastResult_ForecastRunId");
    }
}
