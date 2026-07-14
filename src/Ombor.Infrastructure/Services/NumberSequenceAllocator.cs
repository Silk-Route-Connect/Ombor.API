using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Enums;

namespace Ombor.Infrastructure.Services;

internal sealed class NumberSequenceAllocator(
    IApplicationDbContext context,
    IOrganizationAccessor organizationAccessor) : INumberSequenceAllocator
{
    // Lazy-insert the counter row (serialized by the UPDLOCK/HOLDLOCK probe + the unique index) then
    // increment-and-return in one round trip. Runs on the context connection, so it enlists in the
    // caller's ambient transaction: the row lock is held to commit, which both serializes concurrent
    // allocations for the same series and rolls the increment back if the caller's insert fails — no gaps.
    // The final SELECT must expose a column named "Value" for SqlQueryRaw<int>.
    private const string AllocateSql =
        """
        DECLARE @allocated TABLE (Value int);

        INSERT INTO [NumberSequence] ([OrganizationId], [SeriesType], [LastValue])
        SELECT {0}, {1}, 0
        WHERE NOT EXISTS (
            SELECT 1 FROM [NumberSequence] WITH (UPDLOCK, HOLDLOCK)
            WHERE [OrganizationId] = {0} AND [SeriesType] = {1});

        UPDATE [NumberSequence]
        SET [LastValue] = [LastValue] + 1
        OUTPUT inserted.[LastValue] INTO @allocated ([Value])
        WHERE [OrganizationId] = {0} AND [SeriesType] = {1};

        SELECT [Value] FROM @allocated;
        """;

    public async Task<int> AllocateAsync(NumberSeriesType series, CancellationToken cancellationToken = default)
    {
        var organizationId = organizationAccessor.OrganizationId
            ?? throw new InvalidOperationException("Cannot allocate a document number without an organization context.");

        // Materialize terminally (ToListAsync, not SingleAsync): the multi-statement batch is non-composable,
        // so EF must not wrap it in a subquery. The batch returns exactly one row.
        var allocated = await context.Database
            .SqlQueryRaw<int>(AllocateSql, organizationId, series.ToString())
            .ToListAsync(cancellationToken);

        return allocated.Single();
    }
}
