using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Common;

namespace Ombor.Application.Helpers;

/// <summary>
/// Resolves every foreign id a write request carries through the organization-filtered DbSets before anything is
/// mapped or saved (rule 34). The global query filter only guards reads: without this, an id that exists in another
/// organization satisfies the foreign key and the new row — stamped with the caller's organization — would point
/// into the other tenant's ledger. A missing or foreign id fails as one 400 (<c>validation.failed</c>) with a field
/// error per offending property path, so the client can mark the exact line.
/// </summary>
/// <example>
/// <code>
/// await OwnedReferences.Check()
///     .Require(context.Partners, request.PartnerId, nameof(request.PartnerId))
///     .Require(context.Products, request.Lines.Select((l, i) => (l.ProductId, $"Lines[{i}].ProductId")))
///     .ThrowIfMissingAsync();
/// </code>
/// </example>
internal sealed class OwnedReferences
{
    private readonly List<Func<Task<IEnumerable<ValidationFailure>>>> _checks = [];

    private OwnedReferences()
    {
    }

    public static OwnedReferences Check() => new();

    /// <summary>Requires <paramref name="id"/> (when present) to exist in the caller's organization.</summary>
    public OwnedReferences Require<TEntity>(IQueryable<TEntity> scopedSet, int? id, string propertyName)
        where TEntity : EntityBase
        => id is int value ? Require(scopedSet, [(value, propertyName)]) : this;

    /// <summary>Requires every id to exist in the caller's organization; each carries its own property path.</summary>
    public OwnedReferences Require<TEntity>(
        IQueryable<TEntity> scopedSet,
        IEnumerable<(int Id, string PropertyName)> references)
        where TEntity : EntityBase
    {
        var wanted = references.ToArray();

        if (wanted.Length > 0)
        {
            _checks.Add(() => FindMissingAsync(scopedSet, wanted));
        }

        return this;
    }

    /// <summary>Runs the lookups (one query per entity type) and throws a single 400 listing every missing id.</summary>
    public async Task ThrowIfMissingAsync()
    {
        var failures = new List<ValidationFailure>();

        // Sequential on purpose: the checks share one DbContext, which does not allow concurrent queries.
        foreach (var check in _checks)
        {
            failures.AddRange(await check());
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    private static async Task<IEnumerable<ValidationFailure>> FindMissingAsync<TEntity>(
        IQueryable<TEntity> scopedSet,
        (int Id, string PropertyName)[] wanted)
        where TEntity : EntityBase
    {
        var ids = wanted.Select(w => w.Id).Distinct().ToArray();
        var found = (await scopedSet
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToArrayAsync())
            .ToHashSet();

        return wanted
            .Where(w => !found.Contains(w.Id))
            .Select(w => new ValidationFailure(w.PropertyName, $"{typeof(TEntity).Name} {w.Id} does not exist."));
    }
}
