using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

/// <summary>DR-20: a product referenced by history is delete-gated with 409, and the served IsDeletable agrees.</summary>
public sealed class ProductDeleteGatingTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenProductIsReferenced()
    {
        var productId = await CreateProductAsync(DefaultCategoryId);
        await ReferenceProductAsync(productId);

        await _client.DeleteAsync<ProblemDetails>(GetUrl(productId), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_ShouldServeIsDeletable_ReflectingReferences()
    {
        var freeId = await CreateProductAsync(DefaultCategoryId);
        var referencedId = await CreateProductAsync(DefaultCategoryId);
        await ReferenceProductAsync(referencedId);

        var free = await _client.GetAsync<ProductDto>(GetUrl(freeId));
        var referenced = await _client.GetAsync<ProductDto>(GetUrl(referencedId));

        Assert.True(free.IsDeletable);
        Assert.False(referenced.IsDeletable);
    }

    private async Task ReferenceProductAsync(int productId)
    {
        var partner = new Partner { Name = $"Ref {Guid.NewGuid():N}", Type = PartnerType.Both };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        var transaction = new TransactionRecord
        {
            PartnerId = partner.Id,
            Partner = null!,
            Type = TransactionType.Sale,
            WarehouseId = await EnsureWarehouseAsync(),
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = 100m,
            TotalPaid = 0m,
            Status = TransactionStatus.Open,
            Lines =
            {
                new TransactionLine { ProductId = productId, Product = null!, Transaction = null!, UnitPrice = 100m, Quantity = 1m, Discount = 0m },
            },
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}
