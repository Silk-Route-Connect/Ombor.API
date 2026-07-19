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

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.5, true)]     // fractional base units are valid (decimal quantity)
    [InlineData(0.0, false)]
    [InlineData(-1.0, false)]
    public void LineQuantity_MustBeGreaterThanZero(double quantity, bool expectedValid)
    {
        var request = NewRequest(quantity: (decimal)quantity);

        var result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(50.0, true)]
    [InlineData(-0.01, false)]  // a negative discount would act as a surcharge (rule 37 only clamps positive)
    [InlineData(-100.0, false)]
    public void LineDiscount_CannotBeNegative(double discount, bool expectedValid)
    {
        var request = NewRequest(DiscountType.Fixed, discount: (decimal)discount);

        var result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(100.0, true)]
    [InlineData(100.01, false)]
    [InlineData(150.0, false)]
    public void LinePercentageDiscount_CannotExceed100(double discount, bool expectedValid)
    {
        var request = NewRequest(DiscountType.Percentage, discount: (decimal)discount);

        var result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void LineFixedDiscount_MayExceed100()
    {
        // The <= 100 cap is percentage-only; a fixed discount is a currency amount clamped by the line total (rule 37).
        var request = NewRequest(DiscountType.Fixed, discount: 5_000m);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    private static CreateTransactionRequest NewRequest(
        DiscountType discountType = DiscountType.Percentage,
        decimal quantity = 1m,
        decimal discount = 0m) => new(
        PartnerId: 1,
        Type: TransactionType.Sale,
        Notes: null,
        Lines: [new CreateTransactionLine(ProductId: 1, UnitPrice: 1_000m, Discount: discount, discountType, Quantity: quantity)],
        WalletId: null,
        PaidAmount: 0m,
        Settlements: null,
        Overpayment: OverpaymentHandling.Change,
        Attachments: []);
}
