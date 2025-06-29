using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Abstraction over EF Core’s <see cref="DbContext"/>, exposing only the sets and save capability needed.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Gets or sets the categories set.
    /// </summary>
    DbSet<Category> Categories { get; set; }

    /// <summary>
    /// Gets or sets the products set.
    /// </summary>
    DbSet<Product> Products { get; set; }

    /// <summary>
    /// Gets or sets the product images set.
    /// </summary>
    DbSet<ProductImage> ProductImages { get; set; }

    /// <summary>
    /// Gets or sets the partners set.
    /// </summary>
    DbSet<Partner> Partners { get; set; }

    /// <summary>
    /// Gets or sets the Templates set.
    /// </summary>
    DbSet<Template> Templates { get; set; }

    /// <summary>
    /// Gets or sets the Template Items set.
    /// </summary>
    DbSet<TemplateItem> TemplateItems { get; set; }

    /// <summary>
    /// Gets or sets the Transactions set.
    /// </summary>
    DbSet<TransactionRecord> Transactions { get; set; }

    /// <summary>
    /// Gets or sets the Transaction Line Items set.
    /// </summary>
    DbSet<TransactionLine> TransactionLines { get; set; }

    /// <summary>
    /// Gets or sets the Payments set.
    /// </summary>
    DbSet<Payment> Payments { get; set; }

    /// <summary>
    /// Gets or sets the Payment Allocations set.
    /// </summary>
    DbSet<PaymentAllocation> PaymentAllocations { get; set; }

    /// <summary>
    /// Gets or sets the Payment Attachments set.
    /// </summary>
    DbSet<PaymentAttachment> PaymentAttachments { get; set; }

    /// <summary>
    /// Gets or sets the Partner Ledger Entries set.
    /// </summary>
    DbSet<LedgerEntry> LedgerEntries { get; set; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>
    /// A <see cref="Task{Int32}"/> that returns the number of state entries written.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>
    /// A <see cref="{Int32}"/> that returns the number of state entries written.
    /// </returns>
    int SaveChanges();

    /// <summary>
    /// Gets the <see cref="DatabaseFacade"/> instance that provides access to database-related operations for the.
    /// </summary>
    DatabaseFacade Database { get; }
}
