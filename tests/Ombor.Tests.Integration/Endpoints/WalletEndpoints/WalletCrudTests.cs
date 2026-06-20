using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

public sealed class WalletCrudTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WalletTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldCreateWallet_WithBalanceEqualToOpening()
    {
        // Arrange
        var request = new CreateWalletRequest($"Wallet {Guid.NewGuid():N}", WalletType.Cash, 5_000m);

        // Act
        var response = await _client.PostAsync<WalletDto>(GetUrl(), request);

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal("Cash", response.Type);
        Assert.Equal(5_000m, response.OpeningBalance);
        Assert.Equal(5_000m, response.Balance);   // no transfers yet
        Assert.Equal(0m, response.AdvancesHeld);
        Assert.Equal(5_000m, response.OurMoney);
        Assert.False(response.IsArchived);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenNameAlreadyExists()
    {
        // Arrange
        var name = $"Duplicate {Guid.NewGuid():N}";
        await _client.PostAsync<WalletDto>(GetUrl(), new CreateWalletRequest(name, WalletType.Cash, 0m));

        // Act & Assert
        await _client.PostAsync<ValidationProblemDetails>(
            GetUrl(),
            new CreateWalletRequest(name, WalletType.Card, 0m),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenOpeningBalanceNegative()
        => await _client.PostAsync<ValidationProblemDetails>(
            GetUrl(),
            new CreateWalletRequest($"Wallet {Guid.NewGuid():N}", WalletType.Cash, -1m),
            HttpStatusCode.BadRequest);

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        response.ShouldBeNotFound<Wallet>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldRenameWallet_AndKeepTypeAndOpeningBalance()
    {
        // Arrange
        var walletId = await CreateWalletAsync(openingBalance: 1_000m, type: Domain.Enums.WalletType.Bank);
        var newName = $"Renamed {Guid.NewGuid():N}";

        // Act
        var response = await _client.PutAsync<WalletDto>(GetUrl(walletId), new UpdateWalletRequest(walletId, newName));

        // Assert
        Assert.Equal(newName, response.Name);
        Assert.Equal("Bank", response.Type);          // type unchanged
        Assert.Equal(1_000m, response.OpeningBalance); // opening unchanged
    }

    [Fact]
    public async Task ArchiveThenRestore_ShouldToggleIsArchived_AndKeepListingIt()
    {
        // Arrange
        var walletId = await CreateWalletAsync(openingBalance: 250m);

        // Act — archive
        await _client.PostAsync($"{GetUrl(walletId)}/archive", HttpStatusCode.NoContent);
        var archived = await _client.GetAsync<WalletDto>(GetUrl(walletId));

        // Assert — archived but still readable and still reports its balance (rule 31)
        Assert.True(archived.IsArchived);
        Assert.Equal(250m, archived.Balance);

        // Act — restore
        await _client.PostAsync($"{GetUrl(walletId)}/restore", HttpStatusCode.NoContent);
        var restored = await _client.GetAsync<WalletDto>(GetUrl(walletId));

        // Assert
        Assert.False(restored.IsArchived);
    }
}
