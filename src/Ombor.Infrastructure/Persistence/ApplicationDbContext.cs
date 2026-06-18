using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence;

internal class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantAccessor tenantAccessor)
    : DbContext(options), IApplicationDbContext
{
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<Partner> Partners { get; set; }
    public virtual DbSet<PartnerBalance> PartnerBalances { get; set; }
    public virtual DbSet<Template> Templates { get; set; }
    public virtual DbSet<TemplateItem> TemplateItems { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<Inventory> Inventories { get; set; }
    public virtual DbSet<InventoryItem> InventoryItems { get; set; }
    public virtual DbSet<TransactionRecord> Transactions { get; set; }
    public virtual DbSet<TransactionLine> TransactionLines { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<PaymentComponent> PaymentComponents { get; set; }
    public virtual DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public virtual DbSet<PaymentAttachment> PaymentAttachments { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Tenant> Tenants { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderLine> OrderLines { get; set; }
    public virtual DbSet<Transfer> Transfers { get; set; }
    public virtual DbSet<TransferLine> TransferLines { get; set; }
    public virtual DbSet<AuditEntry> AuditEntries { get; set; }

    /// <summary>
    /// Tenant every <see cref="ITenantScoped"/> query is filtered by. Zero means
    /// "no tenant context" (seeding, design-time tooling) and bypasses the filter.
    /// </summary>
    public int CurrentTenantId => tenantAccessor.TenantId ?? 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        var applyFilter = typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplyTenantQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampTenant();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => CurrentTenantId == 0 || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<TEntity>()
            .HasIndex(e => e.TenantId);
    }

    private void StampTenant()
    {
        var tenantId = tenantAccessor.TenantId;

        if (tenantId is null)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId.Value;
            }
        }
    }
}
