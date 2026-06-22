using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

public sealed class WalletCreatedByTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WalletTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetById_ShouldResolveCreatedBy_ToAuthorDisplayName()
    {
        // Arrange — a wallet authored by a known user (org 1 exists from host seeding, so the FK holds).
        var author = new User
        {
            FirstName = "Dilnoza",
            LastName = "Yusupova",
            PhoneNumber = $"+998{Math.Abs(Guid.NewGuid().GetHashCode())}",
            PasswordHash = "x",
            PasswordSalt = "x",
            Organization = null!,
        };
        _context.Users.Add(author);
        await _context.SaveChangesAsync();

        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = author.Id,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Act
        var dto = await _client.GetAsync<WalletDto>(GetUrl(wallet.Id));

        // Assert — createdBy is the author's display name, not the raw user id
        Assert.Equal("Dilnoza Yusupova", dto.CreatedBy);
    }
}
