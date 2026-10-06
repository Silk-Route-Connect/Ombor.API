using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Domain.Exceptions;

namespace Ombor.Infrastructure.Services;

/// <summary>
/// SQL Server application lock per organization. An application lock instead of row locks or rowversions because a
/// write's checks span tables (stock, payments, transfers, refunds): one named lock covers all of them, and at
/// small-shop volume serializing one organization's writes costs nothing a cashier would notice.
/// </summary>
internal sealed class OrganizationWriteLock(
    IApplicationDbContext context,
    IOrganizationAccessor organizationAccessor,
    IOptions<WriteLockSettings> options) : IOrganizationWriteLock
{
    // Owner 'Transaction': the lock lives exactly as long as the write's transaction, so a commit, a rollback or a
    // dropped connection always releases it. sp_getapplock returns 0/1 when granted, -1 on timeout, -2 when
    // cancelled, -3 as a deadlock victim, -999 for a bad call. The final SELECT must expose a column named "Value".
    private const string AcquireSql =
        """
        DECLARE @result int;
        EXEC @result = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {1};
        SELECT @result AS [Value];
        """;

    private const int InvalidCall = -999;

    public static string ResourceFor(int organizationId) => $"ombor:org:{organizationId}:writes";

    public async Task<IDbContextTransaction> BeginOrgWriteAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = organizationAccessor.OrganizationId
            ?? throw new InvalidOperationException("A money or stock write needs an organization context.");
        var timeoutMilliseconds = options.Value.TimeoutSeconds * 1000;

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Materialize terminally: the batch is non-composable, so EF must not wrap it in a subquery.
            var result = (await context.Database
                .SqlQueryRaw<int>(AcquireSql, ResourceFor(organizationId), timeoutMilliseconds)
                .ToListAsync(cancellationToken))
                .Single();

            if (result == InvalidCall)
            {
                throw new InvalidOperationException($"sp_getapplock rejected the request for organization {organizationId}.");
            }

            if (result < 0)
            {
                throw new ConflictException(
                    "Another operation of this organization is still being recorded. Nothing was saved — send the request again.",
                    ErrorCodes.ConflictBusy);
            }

            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
