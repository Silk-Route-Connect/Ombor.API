using Ombor.Application.Validators.Transaction;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Tests.Unit.Validators;

public sealed class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    [Theory]
    [InlineData(DiscountType.Percentage, true)]
    [InlineData(DiscountType.Fixed, true)]
    // An omitted or default discount type deserializes to 0; the guard must reject it (a clean 400) rather
    // than let it reach the enum mapper as a 500.
    [InlineData((DiscountType)0, false)]
    [InlineData((DiscountType)99, false)]
    public void LineDiscountType_MustBeADefinedEnumValue(DiscountType discountType, bool expectedValid)
    {
        var request = NewRequest(discountType);

        var result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    private static CreateTransactionRequest NewRequest(DiscountType discountType) => new(
        PartnerId: 1,
        Type: TransactionType.Sale,
        Notes: null,
        Lines: [new CreateTransactionLine(ProductId: 1, UnitPrice: 1_000m, Discount: 0m, discountType, Quantity: 1m)],
        WalletId: null,
        PaidAmount: 0m,
        Settlements: null,
        Overpayment: OverpaymentHandling.Change,
        Attachments: []);
}
