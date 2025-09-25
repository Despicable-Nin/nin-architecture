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
        
        builder.Property(s => s.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
            
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(s => s.PrecinctId)
            .IsRequired();
            
        // Configure relationship with Precinct
        builder.HasOne(s => s.Precinct)
            .WithMany(p => p.Streets)
            .HasForeignKey(s => s.PrecinctId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Street_Precinct");
            
        // Create index on PrecinctId for better query performance
        builder.HasIndex(s => s.PrecinctId)
            .HasDatabaseName("IX_Street_PrecinctId");
            
        // Create index on Name for searching
        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Street_Name");
    }
}
