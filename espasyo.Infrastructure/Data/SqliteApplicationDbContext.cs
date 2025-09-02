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
        
        // SQLite-specific configurations can be added here if needed
        // For example, SQLite doesn't support some SQL Server features like certain data types
    }
}
