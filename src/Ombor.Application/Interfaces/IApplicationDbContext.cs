using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Interface for the application database context.
/// </summary>
public interface IApplicationDbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
