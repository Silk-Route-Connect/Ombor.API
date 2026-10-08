using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using EmployeeStatus = Ombor.Domain.Enums.EmployeeStatus;

namespace Ombor.Application.Services.Search;

/// <summary>
/// Loads the master data global search compares, as narrow rows of the current organization (the global filter;
/// archived rows included). Matching happens in memory because Cyrillic↔Latin parity cannot be expressed as a SQL
/// <c>LIKE</c> — fine at small-shop catalog sizes, where the list pages already load these lists whole.
/// </summary>
internal sealed class SearchCandidates(IApplicationDbContext context)
{
    public async Task<SearchCandidate[]> PartnersAsync()
    {
        var rows = await context.Partners
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.CompanyName, p.PhoneNumbers, p.IsArchived })
            .ToArrayAsync();

        return [.. rows.Select(p => new SearchCandidate(
            ActivityEntityKind.Partner,
            p.Id,
            p.Name,
            p.CompanyName ?? p.PhoneNumbers.FirstOrDefault(),
            p.IsArchived,
            [
                new(SearchMatch.Name, p.Name),
                new(SearchMatch.Company, p.CompanyName),
                .. p.PhoneNumbers.Select(phone => new SearchField(SearchMatch.Phone, phone)),
            ]))];
    }

    public async Task<SearchCandidate[]> ProductsAsync()
    {
        var rows = await context.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.SKU, p.Barcode, PackagingBarcode = p.Packaging.Barcode, p.IsArchived })
            .ToArrayAsync();

        return [.. rows.Select(p => new SearchCandidate(
            ActivityEntityKind.Product,
            p.Id,
            p.Name,
            p.SKU,
            p.IsArchived,
            [
                new(SearchMatch.Name, p.Name),
                new(SearchMatch.Sku, p.SKU),
                new(SearchMatch.Barcode, p.Barcode),
                new(SearchMatch.PackagingBarcode, p.PackagingBarcode),
            ]))];
    }

    public async Task<SearchCandidate[]> EmployeesAsync()
    {
        var rows = await context.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.FullName, e.Position, e.Status, e.ContactInfo })
            .ToArrayAsync();

        return [.. rows.Select(e => new SearchCandidate(
            ActivityEntityKind.Employee,
            e.Id,
            e.FullName,
            e.Position,
            // Employees have no archive; a terminated one is flagged the same way.
            e.Status == EmployeeStatus.Terminated,
            [
                new(SearchMatch.Name, e.FullName),
                .. (e.ContactInfo?.PhoneNumbers ?? []).Select(phone => new SearchField(SearchMatch.Phone, phone)),
            ]))];
    }

    public async Task<SearchCandidate[]> WarehousesAsync()
    {
        var rows = await context.Warehouses
            .AsNoTracking()
            .Select(w => new { w.Id, w.Name, w.Location, w.IsArchived })
            .ToArrayAsync();

        return [.. rows.Select(w => new SearchCandidate(
            ActivityEntityKind.Warehouse, w.Id, w.Name, w.Location, w.IsArchived, [new(SearchMatch.Name, w.Name)]))];
    }

    public async Task<SearchCandidate[]> WalletsAsync()
    {
        var rows = await context.Wallets
            .AsNoTracking()
            .Select(w => new { w.Id, w.Name, w.IsArchived })
            .ToArrayAsync();

        return [.. rows.Select(w => new SearchCandidate(
            ActivityEntityKind.Wallet, w.Id, w.Name, null, w.IsArchived, [new(SearchMatch.Name, w.Name)]))];
    }
}
