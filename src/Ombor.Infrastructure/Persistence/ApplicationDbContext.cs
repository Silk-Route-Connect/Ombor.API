using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence;

internal class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<Partner> Partners { get; set; }
    public virtual DbSet<Template> Templates { get; set; }
    public virtual DbSet<TemplateItem> TemplateItems { get; set; }
    public virtual DbSet<Inventory> Inventories { get; set; }
    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
