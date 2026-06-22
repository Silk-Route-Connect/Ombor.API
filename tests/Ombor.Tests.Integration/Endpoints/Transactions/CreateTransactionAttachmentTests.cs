using System.Net;
using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public sealed class CreateTransactionAttachmentTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    [Fact]
    public async Task CreateAsync_ShouldPersistNoteAuthorAndAttachment_ExposedOnDetail()
    {
        // Arrange — a fully-paid Sale carrying a note and one uploaded file.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var content = new byte[] { 1, 2, 3, 4, 5 };
        var request = new CreateTransactionRequest(
            PartnerId: partnerId,
            Type: TransactionType.Sale,
            Notes: "Walk-in customer.",
            Lines: [new CreateTransactionLine(productId, UnitPrice: 5_000m, Discount: 0m, DiscountType.Percentage, Quantity: 1)],
            WalletId: walletId,
            PaidAmount: 5_000m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: [BuildFile("receipt.jpg", "image/jpeg", content)],
            WarehouseId: warehouseId);

        // Act
        var created = await _client.PostAsync<TransactionDto>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.Created);
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(created.Id));

        // Assert — note + author (stamped from the current user) flow through to the detail
        Assert.Equal("Walk-in customer.", detail.Notes);
        Assert.NotNull(detail.CreatedBy);

        // Assert — the uploaded file is stored with raw metadata and a fetchable URL
        var attachment = Assert.Single(detail.Attachments);
        Assert.Equal("receipt.jpg", attachment.Name);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.Equal(content.Length, attachment.SizeBytes);
        Assert.False(string.IsNullOrWhiteSpace(attachment.Url));
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
