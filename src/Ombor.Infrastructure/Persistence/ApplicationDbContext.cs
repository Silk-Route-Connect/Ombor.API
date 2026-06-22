using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence;

internal class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IOrganizationAccessor organizationAccessor)
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
    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<WarehouseItem> WarehouseItems { get; set; }
    public virtual DbSet<TransactionRecord> Transactions { get; set; }
    public virtual DbSet<TransactionLine> TransactionLines { get; set; }
    public virtual DbSet<TransactionAttachment> TransactionAttachments { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Wallet> Wallets { get; set; }
    public virtual DbSet<WalletTransfer> WalletTransfers { get; set; }
    public virtual DbSet<PaymentComponent> PaymentComponents { get; set; }
    public virtual DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public virtual DbSet<PaymentAttachment> PaymentAttachments { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Organization> Organizations { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderLine> OrderLines { get; set; }
    public virtual DbSet<OrderStatusEvent> OrderStatusEvents { get; set; }
    public virtual DbSet<Transfer> Transfers { get; set; }
    public virtual DbSet<TransferLine> TransferLines { get; set; }
    public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }
    public virtual DbSet<OpeningStock> OpeningStocks { get; set; }
    public virtual DbSet<AuditEntry> AuditEntries { get; set; }

    /// <summary>
    /// Organization every <see cref="IOrganizationScoped"/> query is filtered by. Zero means
    /// "no organization context" (seeding, design-time tooling) and bypasses the filter.
    /// </summary>
    public int CurrentOrganizationId => organizationAccessor.OrganizationId ?? 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        var applyFilter = typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplyOrganizationQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType))
            {
                applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampOrganization();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampOrganization();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyOrganizationQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IOrganizationScoped
    {
        var entity = modelBuilder.Entity<TEntity>();

        entity.HasQueryFilter(e => CurrentOrganizationId == 0 || e.OrganizationId == CurrentOrganizationId);

        // Keyless projections (e.g. the PartnerBalance view) carry no table and cannot be indexed;
        // they only need the filter so the view's rows are isolated per organization.
        if (entity.Metadata.FindPrimaryKey() is not null)
        {
            entity.HasIndex(e => e.OrganizationId);
        }
    }

    private void StampOrganization()
    {
        var organizationId = organizationAccessor.OrganizationId;

        if (organizationId is null)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.OrganizationId = organizationId.Value;
            }
        }
    }
}
