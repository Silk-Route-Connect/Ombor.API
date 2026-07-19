using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public sealed class CreatePaymentAttachmentTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldPersistAttachment_ExposedOnCreateAndGet()
    {
        // Arrange — a General expense payment carrying one uploaded file (F18).
        var walletId = await CreateWalletAsync(10_000m);

        var content = new byte[] { 1, 2, 3, 4, 5 };
        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 4_000m, Description: "office supplies", Period: null, Settlements: [],
            Attachments: [BuildFile("receipt.pdf", "application/pdf", content)]);

        // Act
        var created = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());
        var fetched = await _client.GetAsync<PaymentRecordDto>(GetUrl(created.Id));

        // Assert — the file is stored with raw metadata and a fetchable URL, on the create response and a fresh read.
        foreach (var payment in new[] { created, fetched })
        {
            var attachment = Assert.Single(payment.Attachments);
            Assert.Equal("receipt.pdf", attachment.Name);
            Assert.Equal("application/pdf", attachment.ContentType);
            Assert.Equal(content.Length, attachment.SizeBytes);
            Assert.False(string.IsNullOrWhiteSpace(attachment.Url));
        }
    }

    [Fact]
    public async Task PostAsync_ShouldReturnEmptyAttachments_WhenNoneUploaded()
    {
        // Arrange — a payment with no files.
        var walletId = await CreateWalletAsync(10_000m);
        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Expense, null, null, walletId,
            Amount: 1_000m, Description: "misc", Period: null, Settlements: []);

        // Act
        var created = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());

        // Assert
        Assert.Empty(created.Attachments);
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
