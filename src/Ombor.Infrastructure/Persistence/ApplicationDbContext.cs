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
    public virtual DbSet<PartnerBalance> PartnerBalances { get; set; }
    public virtual DbSet<Template> Templates { get; set; }
    public virtual DbSet<TemplateItem> TemplateItems { get; set; }
    public virtual DbSet<TransactionRecord> Transactions { get; set; }
    public virtual DbSet<TransactionLine> TransactionLines { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<PaymentComponent> PaymentComponents { get; set; }
    public virtual DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public virtual DbSet<PaymentAttachment> PaymentAttachments { get; set; }
    public virtual DbSet<UserAccount> UserAccounts { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
