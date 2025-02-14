using Microsoft.EntityFrameworkCore;
using nin.Domain.Entities;

namespace nin.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}