using System.Globalization;
using Ombor.Application.Extensions;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Unit.Extensions;

public sealed class TransactionExtensionsTests
{
    private static readonly DateOnly Today = new(2026, 7, 4);

    [Theory]
    // A non-closed transaction past its due date is overdue.
    [InlineData(TransactionStatus.Open, "2026-07-03", true)]
    [InlineData(TransactionStatus.PartiallyPaid, "2026-07-01", true)]
    // Boundary: due exactly today is not yet overdue.
    [InlineData(TransactionStatus.Open, "2026-07-04", false)]
    // Future due date is not overdue.
    [InlineData(TransactionStatus.Open, "2026-07-10", false)]
    // No due date is never overdue.
    [InlineData(TransactionStatus.Open, null, false)]
    // Closed is never overdue, even past its due date.
    [InlineData(TransactionStatus.Closed, "2026-07-01", false)]
    public void IsOverdue_ReflectsDueDateAndStatus(TransactionStatus stored, string? dueDate, bool expected)
        => Assert.Equal(expected, stored.IsOverdue(Parse(dueDate), Today));

    [Theory]
    [InlineData(TransactionStatus.Open, "2026-07-01", "Overdue")]
    [InlineData(TransactionStatus.PartiallyPaid, "2026-07-01", "Overdue")]  // Overdue overrides PartiallyPaid
    [InlineData(TransactionStatus.Open, "2026-07-10", "Open")]
    [InlineData(TransactionStatus.PartiallyPaid, null, "PartiallyPaid")]
    [InlineData(TransactionStatus.Closed, "2026-07-01", "Closed")]          // Closed stays Closed even past due
    public void ToEffectiveStatusName_OverridesWithOverdue(TransactionStatus stored, string? dueDate, string expected)
        => Assert.Equal(expected, stored.ToEffectiveStatusName(Parse(dueDate), Today));

    [Theory]
    [InlineData(TransactionType.SaleRefund, 42, "S-42")]     // a SaleRefund reverses a Sale
    [InlineData(TransactionType.SupplyRefund, 7, "SP-7")]    // a SupplyRefund reverses a Supply
    public void ToOriginalProvisionalNumber_DerivesFromRefundType(TransactionType refundType, int originalId, string expected)
        => Assert.Equal(expected, refundType.ToOriginalProvisionalNumber(originalId));

    [Theory]
    [InlineData(TransactionType.Sale)]
    [InlineData(TransactionType.Supply)]
    public void ToOriginalProvisionalNumber_IsNull_ForNonRefundType(TransactionType type)
        => Assert.Null(type.ToOriginalProvisionalNumber(99));

    [Fact]
    public void ToOriginalProvisionalNumber_IsNull_WhenNoOriginalId()
        => Assert.Null(TransactionType.SaleRefund.ToOriginalProvisionalNumber(null));

    private static DateOnly? Parse(string? value)
        => value is null ? null : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
