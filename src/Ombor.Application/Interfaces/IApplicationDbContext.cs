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
    /// Gets or sets the Suppliers set.
    /// </summary>
    DbSet<Partner> Suppliers { get; set; }

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
