using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace espasyo.Infrastructure.Data.Configurations
{
    public class PrecinctConfiguration : IEntityTypeConfiguration<Precinct>
    {
        public void Configure(EntityTypeBuilder<Precinct> builder)
        {
            builder.ToTable("Precinct");
            
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
                
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(10);
                
            builder.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Precinct_Code");
                
            builder.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("IX_Precinct_Name");
                
            builder.Property(p => p.Population)
                .IsRequired(false);
                
            builder.Property(p => p.AreaKm2)
                .IsRequired(false)
                .HasColumnType("decimal(10,2)");
                
            builder.Property(p => p.Latitude)
                .IsRequired(false)
                .HasColumnType("decimal(10,8)");
                
            builder.Property(p => p.Longitude)
                .IsRequired(false)
                .HasColumnType("decimal(11,8)");
                
            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(p => p.Description)
                .HasMaxLength(500);
                
            builder.Property(p => p.ContactInfo)
                .HasMaxLength(200);
                
            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("datetime('now')");
                
            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasMany(p => p.Incidents)
                .WithOne(i => i.Precinct)
                .HasForeignKey(i => i.PrecinctId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Incident_Precinct");
                
            // Note: Manpower relationship is configured in ManpowerConfiguration to avoid conflicts
        }
    }
}