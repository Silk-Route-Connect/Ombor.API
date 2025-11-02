using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ombor.Application.Interfaces;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Interceptors;

internal class AuditInterceptor(ITenantProvider tenantProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            UpdateEntities(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext context)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is Organization)
                {
                    continue;
                }

                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.OrganizationId = tenantId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
