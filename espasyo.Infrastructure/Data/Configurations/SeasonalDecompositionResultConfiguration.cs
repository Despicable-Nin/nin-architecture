using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class SeasonalDecompositionResultConfiguration : IEntityTypeConfiguration<SeasonalDecompositionResult>
{
    public void Configure(EntityTypeBuilder<SeasonalDecompositionResult> builder)
    {
        builder.ToTable("SeasonalDecompositionResult");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(r => r.ForecastRunId).IsRequired();
        builder.Property(r => r.Precinct).IsRequired().HasConversion<int>();
        builder.Property(r => r.CrimeType).IsRequired().HasConversion<int>();
        builder.Property(r => r.TrendData).IsRequired().HasColumnType("ntext");
        builder.Property(r => r.SeasonalData).IsRequired().HasColumnType("ntext");
        builder.Property(r => r.ResidualData).IsRequired().HasColumnType("ntext");
        builder.Property(r => r.StrengthTrend).IsRequired().HasColumnType("float");
        builder.Property(r => r.StrengthSeasonal).IsRequired().HasColumnType("float");
        builder.Property(r => r.PeakMonth).IsRequired();
        builder.Property(r => r.TroughMonth).IsRequired();

        builder.HasOne(r => r.ForecastRun)
            .WithMany()
            .HasForeignKey(r => r.ForecastRunId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SeasonalDecompositionResult_ForecastRun");

        builder.HasIndex(r => r.ForecastRunId).HasDatabaseName("IX_SeasonalDecompositionResult_ForecastRunId");
    }
}
