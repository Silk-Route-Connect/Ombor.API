using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public abstract class PaymentTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestsBase(factory, outputHelper)
{
    protected override string GetUrl()
        => "payments";

    protected override string GetUrl(int id)
        => $"payments/{id}";

    protected async Task<int> CreateWalletAsync(decimal openingBalance)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = openingBalance,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner
        {
            Name = $"Partner {Guid.NewGuid():N}",
            Type = PartnerType.Both,
            PhoneNumbers = ["+998900000000"],
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateOpenSaleAsync(int partnerId, decimal total, decimal paid = 0m)
    {
        var sale = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = TransactionType.Sale,
            WarehouseId = await EnsureWarehouseAsync(),
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = total,
            TotalPaid = paid,
            Status = paid > 0 ? TransactionStatus.PartiallyPaid : TransactionStatus.Open,
        };

        _context.Transactions.Add(sale);
        await _context.SaveChangesAsync();

        return sale.Id;
    }
}
