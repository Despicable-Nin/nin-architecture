using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class ManpowerRecommendationConfiguration : IEntityTypeConfiguration<ManpowerRecommendation>
{
    public void Configure(EntityTypeBuilder<ManpowerRecommendation> builder)
    {
        builder.ToTable("ManpowerRecommendation");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(m => m.ForecastRunId).IsRequired();
        builder.Property(m => m.PrecinctId).IsRequired();
        builder.Property(m => m.Shift).IsRequired().HasConversion<int>();
        builder.Property(m => m.RecommendedHeadCount).IsRequired();
        builder.Property(m => m.PredictedWorkloadHours).IsRequired().HasColumnType("real");
        builder.Property(m => m.ComplexityScore).IsRequired().HasColumnType("real");
        builder.Property(m => m.Confidence).IsRequired().HasColumnType("real");
        builder.Property(m => m.Justification).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasOne(m => m.ForecastRun)
            .WithMany()
            .HasForeignKey(m => m.ForecastRunId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ManpowerRecommendation_ForecastRun");

        builder.HasOne(m => m.Precinct)
            .WithMany()
            .HasForeignKey(m => m.PrecinctId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ManpowerRecommendation_Precinct");

        builder.HasIndex(m => m.ForecastRunId).HasDatabaseName("IX_ManpowerRecommendation_ForecastRunId");
    }
}
