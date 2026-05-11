using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using espasyo.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data;

public class SqliteApplicationDbContext(DbContextOptions<SqliteApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Street> Streets { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Manpower> Manpowers { get; set; }
    public DbSet<Precinct> Precincts { get; set; }
    public DbSet<ForecastRun> ForecastRuns { get; set; }
    public DbSet<ForecastResult> ForecastResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Always call base method for Identity

        // Ensure IdentityUserLogin has a primary key
        modelBuilder.Entity<IdentityUserLogin<string>>()
            .HasKey(l => new { l.LoginProvider, l.ProviderKey });

        // Ensure IdentityUserRole has a composite primary key
        modelBuilder.Entity<IdentityUserRole<string>>()
            .HasKey(r => new { r.UserId, r.RoleId });

        // Ensure IdentityUserToken has a composite primary key
        modelBuilder.Entity<IdentityUserToken<string>>()
            .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqliteApplicationDbContext).Assembly);
        
        // SQLite-specific DateTimeOffset configuration
        // Configure proper DateTimeOffset handling for SQLite
        modelBuilder.Entity<Incident>()
            .Property(e => e.TimeStamp)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null, // Convert nullable DateTimeOffset to ISO 8601 string
                v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v)); // Convert back from string

        // Configure DateTimeOffset for Precinct
        modelBuilder.Entity<Precinct>()
            .Property(e => e.CreatedAt)
            .HasConversion(
                v => v.ToString("O"), // Convert DateTimeOffset to ISO 8601 string
                v => DateTimeOffset.Parse(v)); // Convert back from string
                
        modelBuilder.Entity<Precinct>()
            .Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null, // Convert nullable DateTimeOffset to string
                v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v)); // Convert back from string

        // Configure DateTimeOffset for Manpower
        modelBuilder.Entity<Manpower>()
            .Property(e => e.LastUpdated)
            .HasConversion(
                v => v.ToString("O"), // Convert DateTimeOffset to ISO 8601 string
                v => DateTimeOffset.Parse(v)); // Convert back from string

        // Configure DateTimeOffset for ForecastRun
        modelBuilder.Entity<ForecastRun>()
            .Property(e => e.RunAt)
            .HasConversion(
                v => v.ToString("O"),
                v => DateTimeOffset.Parse(v));
            
        // Also configure LockoutEnd from Identity for SQLite
        modelBuilder.Entity<IdentityUser>()
            .Property(e => e.LockoutEnd)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null, // Convert nullable DateTimeOffset to string
                v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v)); // Convert back from string
                
        // Seed initial precincts
        SeedPrecincts(modelBuilder);
    }
    
    private void SeedPrecincts(ModelBuilder modelBuilder)
    {
        var precincts = new[]
        {
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Barangay = Barangay.Alabang, Code = "ALB", Population = 54000, AreaKm2 = 23.5m, IsActive = true, Description = "Commercial and business district", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Barangay = Barangay.Ayala_Alabang, Code = "AAL", Population = 25000, AreaKm2 = 8.2m, IsActive = true, Description = "High-income residential area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Barangay = Barangay.Sucat, Code = "SUC", Population = 42000, AreaKm2 = 15.7m, IsActive = true, Description = "Mixed residential and commercial area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Barangay = Barangay.Poblacion, Code = "POB", Population = 18000, AreaKm2 = 5.3m, IsActive = true, Description = "City center and administrative area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Barangay = Barangay.Putatan, Code = "PUT", Population = 35000, AreaKm2 = 12.8m, IsActive = true, Description = "Residential area with moderate density", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Barangay = Barangay.Tunasan, Code = "TUN", Population = 28000, AreaKm2 = 10.4m, IsActive = true, Description = "Residential with some commercial areas", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Barangay = Barangay.Cupang, Code = "CUP", Population = 22000, AreaKm2 = 8.9m, IsActive = true, Description = "Smaller residential area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Barangay = Barangay.Bayanan, Code = "BAY", Population = 31000, AreaKm2 = 11.6m, IsActive = true, Description = "Residential area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Barangay = Barangay.Buli, Code = "BUL", Population = 26000, AreaKm2 = 9.8m, IsActive = true, Description = "Residential area", CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        };
        
        modelBuilder.Entity<Precinct>().HasData(precincts);
    }
}
