using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class ForecastResultConfiguration : IEntityTypeConfiguration<ForecastResult>
{
    public void Configure(EntityTypeBuilder<ForecastResult> builder)
    {
        builder.ToTable("ForecastResult");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(r => r.ForecastRunId).IsRequired();
        builder.Property(r => r.Precinct).IsRequired().HasConversion<int>();
        builder.Property(r => r.CrimeType).IsRequired().HasConversion<int>();
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
            .HasConstraintName("FK_ForecastResult_ForecastRun");

        builder.HasIndex(r => r.ForecastRunId).HasDatabaseName("IX_ForecastResult_ForecastRunId");
        builder.HasIndex(r => new { r.ForecastRunId, r.Precinct, r.Month, r.Year }).HasDatabaseName("IX_ForecastResult_Run_Precinct_Month_Year");
    }
}
