using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface IApplicationDbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
}
