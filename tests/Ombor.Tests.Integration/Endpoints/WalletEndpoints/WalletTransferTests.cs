using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WalletEndpoints;

public sealed class WalletTransferTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WalletTestsBase(factory, outputHelper)
{
    private string TransfersUrl => $"{GetUrl()}/transfers";

    [Fact]
    public async Task Transfer_ShouldMoveBalance_FromSourceToDestination()
    {
        // Arrange
        var fromId = await CreateWalletAsync(openingBalance: 1_000m);
        var toId = await CreateWalletAsync(openingBalance: 0m);

        // Act
        var transfer = await _client.PostAsync<WalletTransferDto>(
            TransfersUrl,
            new CreateWalletTransferRequest(fromId, toId, 400m, "rent"));

        // Assert — the transfer itself
        Assert.Equal(fromId, transfer.FromWalletId);
        Assert.Equal(toId, transfer.ToWalletId);
        Assert.Equal(400m, transfer.Amount);

        // Assert — both balances moved, atomically (computed, never stored)
        var from = await _client.GetAsync<WalletDto>(GetUrl(fromId));
        var to = await _client.GetAsync<WalletDto>(GetUrl(toId));
        Assert.Equal(600m, from.Balance);
        Assert.Equal(400m, to.Balance);
    }

    [Fact]
    public async Task Transfer_ShouldAppendOneReconcilingOperation_ToEachSide()
    {
        // Arrange
        var fromId = await CreateWalletAsync(openingBalance: 1_000m);
        var toId = await CreateWalletAsync(openingBalance: 0m);

        // Act
        await _client.PostAsync<WalletTransferDto>(
            TransfersUrl,
            new CreateWalletTransferRequest(fromId, toId, 250m, null));

        // Assert — source sees one outgoing op reconciling to its new balance
        var fromOps = await _client.GetAsync<WalletOperationDto[]>($"{GetUrl(fromId)}/operations");
        var fromOp = Assert.Single(fromOps);
        Assert.Equal("Out", fromOp.Direction);
        Assert.Equal(250m, fromOp.Amount);
        Assert.Equal(750m, fromOp.BalanceAfter);

        // Assert — destination sees one incoming op reconciling to its new balance
        var toOps = await _client.GetAsync<WalletOperationDto[]>($"{GetUrl(toId)}/operations");
        var toOp = Assert.Single(toOps);
        Assert.Equal("In", toOp.Direction);
        Assert.Equal(250m, toOp.Amount);
        Assert.Equal(250m, toOp.BalanceAfter);
    }

    [Fact]
    public async Task Transfer_ShouldBeHardBlocked_WhenOverSourceBalance()
    {
        // Arrange
        var fromId = await CreateWalletAsync(openingBalance: 100m);
        var toId = await CreateWalletAsync(openingBalance: 0m);

        // Act & Assert — cannot overdraw
        await _client.PostAsync<ValidationProblemDetails>(
            TransfersUrl,
            new CreateWalletTransferRequest(fromId, toId, 500m, null),
            HttpStatusCode.BadRequest);

        // Assert — nothing moved
        var from = await _client.GetAsync<WalletDto>(GetUrl(fromId));
        Assert.Equal(100m, from.Balance);
    }

    [Fact]
    public async Task Transfer_ShouldReturnBadRequest_WhenSourceEqualsDestination()
    {
        var walletId = await CreateWalletAsync(openingBalance: 100m);

        await _client.PostAsync<ValidationProblemDetails>(
            TransfersUrl,
            new CreateWalletTransferRequest(walletId, walletId, 10m, null),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_ShouldReturnNotFound_WhenWalletMissing()
    {
        var walletId = await CreateWalletAsync(openingBalance: 100m);

        await _client.PostAsync<ProblemDetails>(
            TransfersUrl,
            new CreateWalletTransferRequest(walletId, NonExistentEntityId, 10m, null),
            HttpStatusCode.NotFound);
    }
}
