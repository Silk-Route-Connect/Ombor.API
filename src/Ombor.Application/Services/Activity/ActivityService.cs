using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Activity;
using Ombor.Contracts.Responses.Activity;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services.Activity;

/// <summary>
/// The Activity Log (rules 26–28): the audit rows read back as operations — one per request — newest first, with
/// the actor, what was done, and every record's before/after values.
/// </summary>
internal sealed class ActivityService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ActivityQueries queries) : IActivityService
{
    // A list item carries enough changes to show what happened; a large supply or a legacy seed batch holds
    // hundreds, which the single-operation read returns.
    private const int ChangesPerListItem = 50;
    private const int ChangesPerOperation = 2000;

    public async Task<ActivityPageDto> GetAsync(GetActivityRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var (page, total) = await queries.PageAsync(queries.Matching(request), request.Page, request.PageSize);
        var items = await BuildAsync(page, ChangesPerListItem);

        return new ActivityPageDto(items, total);
    }

    public async Task<ActivityItemDto> GetOperationAsync(GetActivityOperationRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var start = await queries.PageAsync(
            queries.Matching(new GetActivityRequest()).Where(a => a.OperationId == request.OperationId), page: 1, pageSize: 1);

        if (start.Page.Count == 0)
        {
            throw new EntityNotFoundException<AuditEntry>(request.OperationId);
        }

        var items = await BuildAsync(start.Page, ChangesPerOperation);

        return items[0];
    }

    private async Task<ActivityItemDto[]> BuildAsync(List<ActivityOperation> operations, int changesPerOperation)
    {
        if (operations.Count == 0)
        {
            return [];
        }

        var ids = operations.Select(o => o.OperationId).ToList();
        var rows = (await queries.RowsAsync(ids, changesPerOperation))
            .Where(data => ActivityCatalog.IsKnown(data.EntityType))
            .Select(data => new ActivityRow(data))
            .ToList();
        var counts = await queries.CountsAsync(ids);
        var lookups = await ActivityLookups.LoadAsync(context, rows);
        var byOperation = rows.ToLookup(row => row.Data.OperationId);

        return
        [
            .. operations
                .Where(o => byOperation[o.OperationId].Any())
                .Select(o => ActivityAssembler.Build(
                    o.OperationId,
                    [.. byOperation[o.OperationId]],
                    counts.GetValueOrDefault(o.OperationId),
                    lookups)),
        ];
    }
}
