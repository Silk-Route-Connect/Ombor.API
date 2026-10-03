using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ombor.API.ExceptionHandlers;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ErrorHandling;

/// <summary>
/// backend-16: client mistakes must never surface as a 500. A missing body reaches the validator (400), and a
/// database constraint a service guard missed degrades to a 4xx through <see cref="DbUpdateExceptionHandler"/> —
/// exercised here with the real SQL Server errors, captured from the test database.
/// </summary>
public sealed class RequestErrorTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Category;

    protected override string GetUrl(int id) => $"{Routes.Category}/{id}";

    [Theory]
    [InlineData("categories/1")]
    [InlineData("orders/1")]
    [InlineData("warehouses/1")]
    public async Task Put_WithoutBody_ShouldBeBadRequest_NotServerError(string url)
    {
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        await _client.PutAsync(url, content, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPayroll_WithoutBody_ShouldBeBadRequest_NotServerError()
    {
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        await _client.PostAsync<object>($"{Routes.Employee}/1/payrolls", content, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DbUpdateHandler_ShouldMapUniqueViolation_To409Duplicate()
    {
        await using var context = CreateContext();
        var name = $"Wallet {Guid.NewGuid():N}";
        context.Wallets.Add(NewWallet(name));
        await context.SaveChangesAsync();
        context.Wallets.Add(NewWallet(name));

        var (status, code) = await HandleAsync(await CaptureAsync(() => context.SaveChangesAsync()));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("conflict.duplicate", code);
    }

    [Fact]
    public async Task DbUpdateHandler_ShouldMapForeignKeyViolation_To409Referenced()
    {
        await using var context = CreateContext();
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        context.Products.Add(new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            CategoryId = category.Id,
            Category = null!,
        });
        await context.SaveChangesAsync();

        // Untrack the product so EF sends the DELETE instead of refusing it client-side; SQL Server then raises 547.
        context.ChangeTracker.Clear();
        context.Categories.Remove(await context.Categories.FirstAsync(c => c.Id == category.Id));

        var (status, code) = await HandleAsync(await CaptureAsync(() => context.SaveChangesAsync()));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("entity.referenced", code);
    }

    [Fact]
    public async Task DbUpdateHandler_ShouldMapTruncation_To400()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = new string('x', 1_000) });

        var (status, code) = await HandleAsync(await CaptureAsync(() => context.SaveChangesAsync()));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("validation.failed", code);
    }

    private static Wallet NewWallet(string name) => new()
    {
        Name = name,
        Type = WalletType.Cash,
        OpeningBalance = 0m,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static async Task<DbUpdateException> CaptureAsync(Func<Task> save) =>
        await Assert.ThrowsAsync<DbUpdateException>(save);

    private static async Task<(int Status, string? Code)> HandleAsync(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await new DbUpdateExceptionHandler(NullLogger<DbUpdateExceptionHandler>.Instance)
            .TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        httpContext.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(httpContext.Response.Body);

        return (httpContext.Response.StatusCode, body.RootElement.GetProperty("code").GetString());
    }

    private ApplicationDbContext CreateContext()
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(1));
    }
}
