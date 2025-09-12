using espasyo.Domain.Entities;
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
            
        // Also configure LockoutEnd from Identity for SQLite
        modelBuilder.Entity<IdentityUser>()
            .Property(e => e.LockoutEnd)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null, // Convert nullable DateTimeOffset to string
                v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v)); // Convert back from string
    }
}
