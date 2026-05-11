using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class UserForecastPreferenceConfiguration : IEntityTypeConfiguration<UserForecastPreference>
{
    public void Configure(EntityTypeBuilder<UserForecastPreference> builder)
    {
        builder.ToTable("UserForecastPreference");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.DefaultHorizon).IsRequired().HasDefaultValue(6);
        builder.Property(p => p.DefaultConfidenceLevel).IsRequired().HasColumnType("float").HasDefaultValue(0.95);
        builder.Property(p => p.DefaultModelType).IsRequired().HasMaxLength(20).HasDefaultValue("SSA");
        builder.Property(p => p.ShowEnsembleView).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.ShowHotspotTimeline).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.EnabledTimeAnimation).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.PreferredTopN).IsRequired().HasDefaultValue(10);
        builder.Property(p => p.PreferredPrecincts).HasMaxLength(500);
        builder.Property(p => p.PreferredCrimeTypes).HasMaxLength(500);

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("IX_UserForecastPreference_UserId");
    }
}
