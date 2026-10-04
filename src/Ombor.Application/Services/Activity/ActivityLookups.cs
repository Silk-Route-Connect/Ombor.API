using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Application.Services.Activity;

/// <summary>A document's number, type and money figure.</summary>
internal sealed record ActivityDocument(int? Number, DomainTransactionType? TransactionType, PaymentType? PaymentType, decimal? Amount);

/// <summary>What a part row (a line, a stock row, a payment part) points at.</summary>
internal sealed record ActivityPart(int? ProductId, int? WarehouseId, int? WalletId, int? TransactionId);

/// <summary>
/// The numbers, names and amounts one Activity Log page needs, loaded with one query per entity type for the whole
/// page — never per row. Every set goes through the organization filter.
/// </summary>
internal sealed class ActivityLookups
{
    private readonly Dictionary<string, HashSet<int>> _wanted = [];
    private readonly Dictionary<(string, int), ActivityPart> _parts = [];
    private readonly Dictionary<(string, int), ActivityDocument> _documents = [];
    private readonly Dictionary<(string, int), string> _names = [];

    public static async Task<ActivityLookups> LoadAsync(IApplicationDbContext context, IEnumerable<ActivityRow> rows)
    {
        var lookups = new ActivityLookups();

        foreach (var row in rows)
        {
            lookups.Want(row.EntityType, row.EntityId);

            if (row.Data is { ParentEntityType: { } parentType, ParentEntityId: { } parentId })
            {
                lookups.Want(parentType, parentId);
            }

            if (row.Data.UserId is { } userId)
            {
                lookups.Want(nameof(User), userId);
            }

            foreach (var (column, target) in ActivityCatalog.References)
            {
                lookups.Want(target, ReferencedIds(row, column));
            }
        }

        await lookups.LoadPartsAsync(context);
        await lookups.LoadDocumentsAsync(context);
        await lookups.LoadNamesAsync(context);

        return lookups;
    }

    public ActivityPart? Part(string entityType, int id) => _parts.GetValueOrDefault((entityType, id));

    public ActivityDocument? Document(string entityType, int id) => _documents.GetValueOrDefault((entityType, id));

    public string? Name(string entityType, int id) => _names.GetValueOrDefault((entityType, id));

    private static IEnumerable<int> ReferencedIds(ActivityRow row, string column) =>
        new[] { row.Old.GetValueOrDefault(column), row.New.GetValueOrDefault(column) }
            .Where(value => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _))
            .Select(value => value.GetInt32());

    private void Want(string entityType, params int[] ids) => Want(entityType, ids.AsEnumerable());

    private void Want(string entityType, IEnumerable<int> ids)
    {
        if (!_wanted.TryGetValue(entityType, out var set))
        {
            _wanted[entityType] = set = [];
        }

        set.UnionWith(ids.Where(id => id > 0));
    }

    private List<int> Ids(string entityType) => _wanted.TryGetValue(entityType, out var set) ? [.. set] : [];

    private async Task LoadPartsAsync(IApplicationDbContext context)
    {
        await AddPartsAsync(nameof(TransactionLine), ids => context.TransactionLines
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, null, null, null))));
        await AddPartsAsync(nameof(OrderLine), ids => context.OrderLines
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, null, null, null))));
        await AddPartsAsync(nameof(TemplateItem), ids => context.TemplateItems
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, null, null, null))));
        await AddPartsAsync(nameof(TransferLine), ids => context.TransferLines
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, null, null, null))));
        await AddPartsAsync(nameof(WarehouseItem), ids => context.WarehouseItems
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, x.WarehouseId, null, null))));
        await AddPartsAsync(nameof(StockAdjustment), ids => context.StockAdjustments
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, x.WarehouseId, null, null))));
        await AddPartsAsync(nameof(OpeningStock), ids => context.OpeningStocks
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(x.ProductId, x.WarehouseId, null, null))));
        await AddPartsAsync(nameof(PaymentComponent), ids => context.PaymentComponents
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(null, null, x.WalletId, null))));
        await AddPartsAsync(nameof(PaymentAllocation), ids => context.PaymentAllocations
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdPart(x.Id, new ActivityPart(null, null, null, x.TransactionId))));
    }

    private async Task AddPartsAsync(string entityType, Func<List<int>, IQueryable<IdPart>> query)
    {
        var ids = Ids(entityType);

        if (ids.Count == 0)
        {
            return;
        }

        foreach (var (id, part) in await query(ids).AsNoTracking().ToListAsync())
        {
            _parts[(entityType, id)] = part;

            Want(nameof(Product), part.ProductId ?? 0);
            Want(nameof(Warehouse), part.WarehouseId ?? 0);
            Want(nameof(Wallet), part.WalletId ?? 0);
            Want(ActivityCatalog.Transaction, part.TransactionId ?? 0);
        }
    }

    private async Task LoadDocumentsAsync(IApplicationDbContext context)
    {
        var transactions = Ids(ActivityCatalog.Transaction);
        foreach (var x in await context.Transactions.AsNoTracking().Where(x => transactions.Contains(x.Id))
                     .Select(x => new { x.Id, x.Number, x.Type, x.TotalDue }).ToListAsync())
        {
            _documents[(ActivityCatalog.Transaction, x.Id)] = new(x.Number, x.Type, null, x.TotalDue);
        }

        var payments = Ids(ActivityCatalog.Payment);
        foreach (var x in await context.Payments.AsNoTracking().Where(x => payments.Contains(x.Id))
                     .Select(x => new { x.Id, x.Number, x.Type, Amount = x.Components.Sum(c => c.Amount) }).ToListAsync())
        {
            _documents[(ActivityCatalog.Payment, x.Id)] = new(x.Number, null, x.Type, x.Amount);
        }

        var orders = Ids(ActivityCatalog.Order);
        foreach (var x in await context.Orders.AsNoTracking().Where(x => orders.Contains(x.Id))
                     .Select(x => new { x.Id, x.OrderNumber, x.TotalAmount }).ToListAsync())
        {
            _documents[(ActivityCatalog.Order, x.Id)] = new(x.OrderNumber, null, null, x.TotalAmount);
        }

        var walletTransfers = Ids(nameof(WalletTransfer));
        foreach (var x in await context.WalletTransfers.AsNoTracking().Where(x => walletTransfers.Contains(x.Id))
                     .Select(x => new { x.Id, x.Amount }).ToListAsync())
        {
            _documents[(nameof(WalletTransfer), x.Id)] = new(null, null, null, x.Amount);
        }

        var adjustments = Ids(nameof(StockAdjustment));
        foreach (var x in await context.StockAdjustments.AsNoTracking().Where(x => adjustments.Contains(x.Id))
                     .Select(x => new { x.Id, x.Quantity, x.UnitCost }).ToListAsync())
        {
            _documents[(nameof(StockAdjustment), x.Id)] = new(null, null, null, Math.Round(x.Quantity * x.UnitCost, 2));
        }

        var openingStocks = Ids(nameof(OpeningStock));
        foreach (var x in await context.OpeningStocks.AsNoTracking().Where(x => openingStocks.Contains(x.Id))
                     .Select(x => new { x.Id, x.Quantity, x.UnitCost }).ToListAsync())
        {
            _documents[(nameof(OpeningStock), x.Id)] = new(null, null, null, Math.Round(x.Quantity * x.UnitCost, 2));
        }
    }

    private async Task LoadNamesAsync(IApplicationDbContext context)
    {
        await AddNamesAsync(nameof(Product), ids => context.Products.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Partner), ids => context.Partners.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Wallet), ids => context.Wallets.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Warehouse), ids => context.Warehouses.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Category), ids => context.Categories.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Template), ids => context.Templates.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(Employee), ids => context.Employees.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.FullName)));
        await AddNamesAsync(nameof(Organization), ids => context.Organizations.Where(x => ids.Contains(x.Id)).Select(x => new IdName(x.Id, x.Name)));
        await AddNamesAsync(nameof(User), ids => context.Users
            .Where(x => ids.Contains(x.Id))
            .Select(x => new IdName(x.Id, (x.FirstName + " " + x.LastName).Trim())));
    }

    private async Task AddNamesAsync(string entityType, Func<List<int>, IQueryable<IdName>> query)
    {
        var ids = Ids(entityType);

        if (ids.Count == 0)
        {
            return;
        }

        foreach (var (id, name) in await query(ids).AsNoTracking().ToListAsync())
        {
            _names[(entityType, id)] = name;
        }
    }

    private sealed record IdPart(int Id, ActivityPart Part);

    private sealed record IdName(int Id, string Name);
}
