using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public sealed class PaymentTransactionEchoTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    [Fact]
    public async Task Payment_ShouldEchoSettledTransactionNotesAndAttachments()
    {
        // Arrange — a fully-paid Sale carrying a note + one file; the inline payment settles it.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var content = new byte[] { 1, 2, 3, 4, 5 };
        var request = new CreateTransactionRequest(
            PartnerId: partnerId,
            Type: TransactionType.Sale,
            Notes: "Delivered to front desk.",
            Lines: [new CreateTransactionLine(productId, UnitPrice: 5_000m, Discount: 0m, DiscountType.Percentage, Quantity: 1)],
            WalletId: walletId,
            PaidAmount: 5_000m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: [BuildFile("receipt.jpg", "image/jpeg", content)],
            WarehouseId: warehouseId);

        var created = await _client.PostAsync<TransactionDto>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.Created);

        // The inline payment that settled this transaction.
        var paymentId = await _context.PaymentAllocations.AsNoTracking()
            .Where(a => a.TransactionId == created.Id)
            .Select(a => a.PaymentId)
            .FirstAsync();

        // Act
        var payment = await _client.GetAsync<PaymentRecordDto>($"payments/{paymentId}");

        // Assert — the settled transaction's note + attachment are echoed onto the payment (read-side, not duplicated).
        Assert.Equal("Delivered to front desk.", payment.TransactionNotes);
        var echoed = Assert.Single(payment.TransactionAttachments);
        Assert.Equal("receipt.jpg", echoed.Name);
        Assert.Equal("image/jpeg", echoed.ContentType);
        Assert.Equal(content.Length, echoed.SizeBytes);
        Assert.False(string.IsNullOrWhiteSpace(echoed.Url));

        // The payment's OWN attachments stay separate — nothing was uploaded to the payment itself.
        Assert.Empty(payment.Attachments);
    }

    [Fact]
    public async Task Payment_ShouldLeaveEchoEmpty_WhenItSettlesNoTransaction()
    {
        // Arrange — a standalone deposit that settles nothing.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();

        var deposit = new CreatePaymentRecordRequest(
            PaymentType.Deposit, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 1_000m, Description: null, Period: null, Settlements: []);
        var payment = await _client.PostAsync<PaymentRecordDto>("payments", deposit.ToMultipartFormData());

        // Act
        var fetched = await _client.GetAsync<PaymentRecordDto>($"payments/{payment.Id}");

        // Assert — no settled transaction, so nothing to echo.
        Assert.Null(fetched.TransactionNotes);
        Assert.Empty(fetched.TransactionAttachments);
    }

    private static FormFile BuildFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);

        return new FormFile(stream, 0, content.Length, "Attachments", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
