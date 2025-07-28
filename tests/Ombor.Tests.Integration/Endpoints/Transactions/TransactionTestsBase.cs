using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public abstract class TransactionsTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper output)
        : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Transaction;
    protected override string GetUrl(int id) => $"{Routes.Transaction}/{id}";

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner
        {
            Name = _faker.Person.FullName,
            Type = PartnerType.Both
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateOpenTransactionAsync(int partnerId, decimal due, decimal paid, TransactionType type = TransactionType.Sale)
    {
        var openTransaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = due,
            TotalPaid = paid,
            Status = TransactionStatus.PartiallyPaid,
        };
        _context.Transactions.Add(openTransaction);
        await _context.SaveChangesAsync();

        return openTransaction.Id;
    }

    protected async Task<int> CreateAdvancePaymentAsync(int partnerId, decimal amount, PaymentDirection direction = PaymentDirection.Income)
    {
        var payment = new Payment
        {
            DateUtc = DateTimeOffset.UtcNow,
            Notes = "Advance payment for test",
            Type = PaymentType.General,
            Direction = direction,
            PartnerId = partnerId,
        };
        payment.Components.Add(new PaymentComponent
        {
            Amount = amount,
            Currency = "UZS",
            ExchangeRate = 1,
            Method = PaymentMethod.Cash,
            Payment = null!, // will be set by EF
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Amount = amount,
            Type = PaymentAllocationType.AdvancePayment,
            Payment = null!,
            Transaction = null!
        });

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return payment.Id;
    }
}
