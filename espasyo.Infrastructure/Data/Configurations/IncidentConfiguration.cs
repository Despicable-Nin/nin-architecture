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
        
        builder.Property("_latitude")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Latitude");
        
        builder.Property("_longitude")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Longitude");
    }
}