using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
{
    public void Configure(EntityTypeBuilder<AnalysisRun> builder)
    {
        builder.ToTable("AnalysisRun");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(a => a.ParametersJson).IsRequired();
        builder.Property(a => a.ClusterGroupsJson).IsRequired();
        builder.Property(a => a.QualityMetricsJson).IsRequired();
        builder.Property(a => a.CreatedById).IsRequired().HasMaxLength(450);
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("IX_AnalysisRun_CreatedAt");
    }
}
