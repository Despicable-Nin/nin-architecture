using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incident");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CaseId).IsUnique();
        
        builder.Property(p => p.TimeStamp)
            .HasColumnName("IncidentDateTime")
            .IsRequired();

        builder.Property("_latitude")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Latitude");
        
        builder.Property("_longitude")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Longitude");
        
        builder.Property("_timestampInUnix")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("TimestampInUnix")
            .IsRequired();
        
        builder.Property("_year")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Year")
            .IsRequired();

        builder.Property("_month")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Month")
            .IsRequired();

        builder.Property("_timeOfDay")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("TimeOfDay")
            .IsRequired();
    }
}