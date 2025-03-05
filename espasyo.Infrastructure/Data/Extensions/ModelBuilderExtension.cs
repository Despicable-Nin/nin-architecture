using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Extensions;

public static class ModelBuilderExtension
{
    public static ModelBuilder AddSeedIdentityUserAndRole(this ModelBuilder modelBuilder)
    {
        // Use static GUIDs instead of dynamic values
        var adminRoleId = "B5D3D9D6-53E5-44C6-BD92-5C77B3A6F5E1";
        var userRoleId = "A1F41CC9-07C3-4B0C-9EBF-8D638FA3F763";
        var adminUserId = "D2A5B7B8-91A6-4E6A-8921-0DBBC5E8A5D4";

        var roles = new List<IdentityRole>
        {
            new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER" }
        };

        modelBuilder.Entity<IdentityRole>().HasData(roles);

        var adminUser = new IdentityUser
        {
            Id = adminUserId,
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            EmailConfirmed = true,
            PasswordHash = "$2y$10$ymwOJBzLSdKa/YMtxRFg6.BZUuJ4SO5Ouhn9weEF/UcrwFyTb3CNq", //Admin@123
            SecurityStamp = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()"
        };

        modelBuilder.Entity<IdentityUser>().HasData(adminUser);

        modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
        {
            UserId = adminUserId,
            RoleId = adminRoleId
        });

        return modelBuilder;
    }
}
