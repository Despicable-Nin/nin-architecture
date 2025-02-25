using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations;

public class StreetConfiguration : IEntityTypeConfiguration<Street>
{
    public void Configure(EntityTypeBuilder<Street> builder)
    {
        builder.ToTable("Street");
        builder.HasKey(x => x.Id);
        builder.Property("_barangay")
            .UsePropertyAccessMode(propertyAccessMode: PropertyAccessMode.Field)
            .HasColumnName("Barangay");
    }
}