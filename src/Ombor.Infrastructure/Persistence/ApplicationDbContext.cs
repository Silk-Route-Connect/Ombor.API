using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence;

internal class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<UserAccount, IdentityRole<int>, int>(options), IApplicationDbContext
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
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>().ToTable("UserAccount");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Role");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRole");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaim");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogin");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaim");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserToken");
    }
}
