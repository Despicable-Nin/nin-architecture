using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Extensions;

public static class ModelBuilderExtension
{
    public static ModelBuilder AddSeedIdentityUserAndRole(this ModelBuilder modelBuilder)
    {
        // Explicitly set primary keys for Identity tables
        modelBuilder.Entity<IdentityUserRole<string>>()
            .HasKey(r => new { r.UserId, r.RoleId });

        modelBuilder.Entity<IdentityUserLogin<string>>()
            .HasKey(l => new { l.LoginProvider, l.ProviderKey });

        modelBuilder.Entity<IdentityUserToken<string>>()
            .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        
        // Define roles
        var adminRoleId = Guid.NewGuid().ToString();
        var userRoleId = Guid.NewGuid().ToString();

        var roles = new List<IdentityRole>
        {
            new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER" }
        };

        modelBuilder.Entity<IdentityRole>().HasData(roles);

        // Define a user
        var adminUserId = Guid.NewGuid().ToString();
        var hasher = new PasswordHasher<IdentityUser>();

        var adminUser = new IdentityUser
        {
            Id = adminUserId,
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            EmailConfirmed = true,
            PasswordHash = hasher.HashPassword(null, "Admin@123"), // Set a secure password
            SecurityStamp = Guid.NewGuid().ToString()
        };

        modelBuilder.Entity<IdentityUser>().HasData(adminUser);

        // Assign admin role to the user
        var userRoles = new List<IdentityUserRole<string>>
        {
            new IdentityUserRole<string> { UserId = adminUserId, RoleId = adminRoleId }
        };

        modelBuilder.Entity<IdentityUserRole<string>>().HasData(userRoles);
        
        return modelBuilder;
    }
}