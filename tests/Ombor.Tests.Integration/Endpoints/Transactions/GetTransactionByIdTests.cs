using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public sealed class GetTransactionByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    private const string AuthorFirstName = "Aziz";
    private const string AuthorLastName = "Karimov";
    private const string NoteText = "Counter sale, partial payment.";
    private const string AttachmentName = "receipt.pdf";
    private const string AttachmentContentType = "application/pdf";
    private const long AttachmentSizeBytes = 2_048;
    private const string AttachmentUrl = "/files/transactions/originals/receipt.pdf";

    // A future due date keeps this shape-focused sale PartiallyPaid (not Overdue) regardless of when the test runs.
    private static readonly DateOnly SettledSaleDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

    [Fact]
    public async Task GetById_ShouldReturnFullDetail_WithLinesAndSettlingPayments()
    {
        // Arrange — a partially-paid Sale (10,000 due, 4,000 settled through a Cash wallet), one line.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        var (transactionId, walletName) = await SeedSettledSaleAsync(partnerId, warehouseId, productId);

        // Act
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(transactionId));

        // Assert — header (number/direction match the debts read model the row deep-links from)
        Assert.Equal(transactionId, detail.Id);
        Assert.Equal($"S-{transactionId}", detail.Number);
        Assert.Equal("Sale", detail.Type);
        Assert.Equal("Receivable", detail.Direction);
        Assert.Equal("PartiallyPaid", detail.Status);
        Assert.Equal(partnerId, detail.PartnerId);
        Assert.Equal(warehouseId, detail.WarehouseId);
        Assert.Equal(SettledSaleDueDate, detail.DueDate);
        Assert.Equal(10_000m, detail.TotalDue);
        Assert.Equal(4_000m, detail.TotalPaid);
        Assert.Equal(6_000m, detail.Remaining);

        // Assert — lines
        var line = Assert.Single(detail.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(10_000m, line.Total);

        // Assert — the embedded settling payment carries the wallet
        var payment = Assert.Single(detail.Payments);
        Assert.Equal(transactionId, payment.TransactionId);
        Assert.Equal(4_000m, payment.Amount);
        Assert.Equal(walletName, payment.WalletName);
        Assert.Equal("Cash", payment.WalletType);

        // Assert — audit card (author display name) + note
        Assert.Equal($"{AuthorFirstName} {AuthorLastName}", detail.CreatedBy);
        Assert.Equal(NoteText, detail.Notes);

        // Assert — attachment carries raw values (client derives icon + formats size)
        var attachment = Assert.Single(detail.Attachments);
        Assert.Equal(AttachmentName, attachment.Name);
        Assert.Equal(AttachmentContentType, attachment.ContentType);
        Assert.Equal(AttachmentSizeBytes, attachment.SizeBytes);
        Assert.Equal(AttachmentUrl, attachment.Url);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenTransactionMissing()
    {
        await _client.GetAsync<ProblemDetails>(GetUrl(NonExistentEntityId), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldComputeOverdue_ForPastDueTransaction()
    {
        // Arrange — an Open sale two days past due.
        var partnerId = await CreatePartnerAsync();
        var id = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Open,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2));

        // Act
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(id));

        // Assert
        Assert.Equal("Overdue", detail.Status);
    }

    [Fact]
    public async Task GetById_ShouldServeOriginalTransactionNumber_ForRefund()
    {
        // Arrange — a Sale and a SaleRefund that reverses it.
        var partnerId = await CreatePartnerAsync();
        var saleId = await SeedTransactionAsync(partnerId, TransactionType.Sale, TransactionStatus.Closed);
        var refundId = await SeedTransactionAsync(partnerId, TransactionType.SaleRefund, TransactionStatus.Open, originalTransactionId: saleId);

        // Act
        var refund = await _client.GetAsync<TransactionDetailDto>(GetUrl(refundId));
        var sale = await _client.GetAsync<TransactionDetailDto>(GetUrl(saleId));

        // Assert — the refund carries the original's document number; a non-refund has none.
        Assert.Equal(saleId, refund.OriginalTransactionId);
        Assert.Equal($"S-{saleId}", refund.OriginalTransactionNumber);
        Assert.Null(sale.OriginalTransactionNumber);
    }

    private async Task<(int transactionId, string walletName)> SeedSettledSaleAsync(int partnerId, int warehouseId, int productId)
    {
        // The author resolves to a display name on read; org 1 exists (seeded host), so the FK holds.
        var author = new User
        {
            FirstName = AuthorFirstName,
            LastName = AuthorLastName,
            PhoneNumber = $"+998{Math.Abs(Guid.NewGuid().GetHashCode())}",
            PasswordHash = "x",
            PasswordSalt = "x",
            Organization = null!,
        };
        _context.Users.Add(author);
        await _context.SaveChangesAsync();

        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = TransactionType.Sale,
            WarehouseId = warehouseId,
            DateUtc = DateTimeOffset.UtcNow,
            DueDate = SettledSaleDueDate,
            TotalDue = 10_000m,
            TotalPaid = 4_000m,
            Status = TransactionStatus.PartiallyPaid,
            Notes = NoteText,
            CreatedById = author.Id,
        };
        transaction.Lines.Add(new TransactionLine
        {
            ProductId = productId,
            Product = null!,
            Transaction = null!,
            UnitPrice = 1_000m,
            Discount = 0m,
            DiscountType = DiscountType.Fixed,
            Quantity = 10m,
        });
        transaction.Attachments.Add(new TransactionAttachment
        {
            Transaction = null!,
            FileId = "receipt-key.pdf",
            FileName = AttachmentName,
            ContentType = AttachmentContentType,
            SizeBytes = AttachmentSizeBytes,
            Url = AttachmentUrl,
        });
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            Number = $"P-{Guid.NewGuid():N}",
            Type = PaymentType.Transaction,
            Direction = PaymentDirection.Income,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = partnerId,
            WalletId = wallet.Id,
        };
        payment.Components.Add(new PaymentComponent
        {
            Payment = payment,
            SourceType = PaymentSourceType.Wallet,
            WalletId = wallet.Id,
            Amount = 4_000m,
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Payment = payment,
            TransactionId = transaction.Id,
            Type = PaymentAllocationType.TransactionSettlement,
            Amount = 4_000m,
        });
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return (transaction.Id, wallet.Name);
    }
}
