using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Wallet;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainAllocationType = Ombor.Domain.Enums.PaymentAllocationType;
using EmployeeStatus = Ombor.Domain.Enums.EmployeeStatus;

namespace Ombor.Tests.Integration.Endpoints.Concurrency;

/// <summary>
/// DR-25 and the settlement caps under parallel requests: a wallet is never overdrawn, a document is never settled past
/// its total, and the stored TotalPaid always equals the sum of its settling allocations (backend-7).
/// </summary>
public sealed class ParallelMoneyWriteTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ConcurrencyTestsBase(factory, output)
{
    private const string PaymentsRoute = "payments";

    [Fact]
    public async Task ParallelExpenses_FromOneWallet_NeverOverdrawIt()
    {
        var walletId = await CreateWalletAsync(openingBalance: 1_000m);

        var outcomes = await FireAsync(_ => Form(PaymentsRoute, new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 300m, Description: "rent", Period: null, Settlements: []).ToMultipartFormData()));

        AssertOutcomes(outcomes, succeeded: 3, HttpStatusCode.Created, HttpStatusCode.BadRequest, "wallet.insufficient_balance");
        Assert.Equal(100m, await WalletBalanceAsync(walletId));
    }

    [Fact]
    public async Task ParallelPayrolls_FromOneWallet_NeverOverdrawIt()
    {
        var walletId = await CreateWalletAsync(openingBalance: 1_000m);
        var employee = new Employee
        {
            FullName = $"Employee {Guid.NewGuid():N}",
            Position = "Cashier",
            Salary = 300m,
            Status = EmployeeStatus.Active,
            DateOfEmployment = new DateOnly(2026, 1, 1),
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var outcomes = await FireAsync(_ => Json($"{Routes.Employee}/{employee.Id}/payrolls", new
        {
            employeeId = employee.Id,
            walletId,
            amount = 300m,
            period = "2026-09",
            notes = (string?)null,
        }));

        AssertOutcomes(outcomes, succeeded: 3, HttpStatusCode.Created, HttpStatusCode.BadRequest, "wallet.insufficient_balance");
        Assert.Equal(100m, await WalletBalanceAsync(walletId));
    }

    [Fact]
    public async Task ParallelSuppliesPaidFromOneWallet_NeverOverdrawIt()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync(openingBalance: 1_000m);
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();

        var outcomes = await FireAsync(_ => Form(
            Routes.Transaction,
            TransactionRequestFactory.Supply(partnerId, productId, warehouseId, due: 300m, walletId, paidAmount: 300m)
                .ToMultipartFormData()));

        // A blocked payment rolls its supply back too: only the paid supplies moved stock in.
        AssertOutcomes(outcomes, succeeded: 3, HttpStatusCode.Created, HttpStatusCode.BadRequest, "wallet.insufficient_balance");
        Assert.Equal(100m, await WalletBalanceAsync(walletId));
        Assert.Equal(3m, await StockOfAsync(warehouseId, productId));
    }

    [Fact]
    public async Task ParallelSettlements_OfOneSale_KeepTotalPaidEqualToItsAllocations()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var saleId = await CreateOpenTransactionAsync(partnerId, due: 1_000m, paid: 0m);

        var outcomes = await FireAsync(_ => Form(PaymentsRoute, new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 150m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 150m)]).ToMultipartFormData()));

        // Six payments fit (900); a seventh would settle 1 050 of a 1 000 sale.
        AssertOutcomes(outcomes, succeeded: 6, HttpStatusCode.Created, HttpStatusCode.BadRequest, "validation.failed");

        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == saleId);
        var allocated = await _context.PaymentAllocations
            .Where(a => a.TransactionId == saleId && a.Type == DomainAllocationType.TransactionSettlement)
            .SumAsync(a => a.Amount);
        Assert.Equal(900m, sale.TotalPaid);
        Assert.Equal(allocated, sale.TotalPaid);
        Assert.Equal(900m, await WalletBalanceAsync(walletId));
    }

    [Fact]
    public async Task ParallelWalletTransfers_NeverOverdrawTheSource()
    {
        var sourceId = await CreateWalletAsync(openingBalance: 500m);
        var destinationId = await CreateWalletAsync();

        var outcomes = await FireAsync(_ => Json($"{Routes.Wallet}/transfers", new
        {
            fromWalletId = sourceId,
            toWalletId = destinationId,
            amount = 100m,
            note = (string?)null,
        }));

        AssertOutcomes(outcomes, succeeded: 5, HttpStatusCode.Created, HttpStatusCode.BadRequest, "wallet.insufficient_balance");
        Assert.Equal(0m, await WalletBalanceAsync(sourceId));
        Assert.Equal(500m, await WalletBalanceAsync(destinationId));
    }

    private async Task<decimal> WalletBalanceAsync(int walletId) =>
        (await _client.GetAsync<WalletDto>($"{Routes.Wallet}/{walletId}")).Balance;
}
