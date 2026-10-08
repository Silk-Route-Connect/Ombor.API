using FluentValidation;
using Ombor.Application.Services;
using Ombor.Application.Services.Reports;
using Ombor.Contracts.Enums;
using Ombor.Tests.Common.Helpers;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Tests.Unit.Services;

/// <summary>The report period, bucket and figure rules (scope-8).</summary>
public sealed class ReportRulesTests
{
    // 04.10.2026 03:30 in Tashkent — still 03.10 in UTC.
    private static readonly BusinessClock Clock =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 3, 22, 30, 0, TimeSpan.Zero)));

    [Fact]
    public void Range_ShouldDefaultToTheCurrentLocalMonth_AndBoundItByLocalMidnights()
    {
        var range = ReportRange.Resolve(null, null, Clock);

        Assert.Equal(new DateOnly(2026, 10, 1), range.From);
        Assert.Equal(new DateOnly(2026, 10, 4), range.To);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 19, 0, 0, TimeSpan.Zero), range.StartUtc.ToUniversalTime());
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 19, 0, 0, TimeSpan.Zero), range.EndUtc.ToUniversalTime());
        Assert.Equal(4, range.Days.Count());
    }

    [Fact]
    public void Range_ShouldDefaultFromToTheFirstOfTheGivenMonth()
    {
        var range = ReportRange.Resolve(null, new DateOnly(2026, 2, 17), Clock);

        Assert.Equal(new DateOnly(2026, 2, 1), range.From);
    }

    [Fact]
    public void Range_ShouldRejectAReversedOrOverLongPeriod()
    {
        var reversed = Assert.Throws<ValidationException>(() =>
            ReportRange.Resolve(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 4), Clock));
        Assert.Equal("From", Assert.Single(reversed.Errors).PropertyName);

        var tooLong = Assert.Throws<ValidationException>(() =>
            ReportRange.Resolve(new DateOnly(2023, 1, 1), new DateOnly(2026, 10, 4), Clock));
        Assert.Equal("To", Assert.Single(tooLong.Errors).PropertyName);
    }

    [Theory]
    [InlineData(ReportGroupBy.Day, "2026-10-04", "2026-10-04")]
    [InlineData(ReportGroupBy.Week, "2026-10-04", "2026-09-28")] // a Sunday belongs to the week of Monday 28.09
    [InlineData(ReportGroupBy.Week, "2026-09-28", "2026-09-28")]
    [InlineData(ReportGroupBy.Month, "2026-10-04", "2026-10")]
    public void Bucket_KeyShouldBeTheDayTheMondayOrTheMonth(ReportGroupBy groupBy, string date, string expected)
    {
        Assert.Equal(expected, ReportBuckets.KeyOf(groupBy, DateOnly.Parse(date)));
    }

    [Fact]
    public void Figures_ShouldNetRefundsOutOfRevenueQuantityAndCost()
    {
        var figures = LineFigures.Of(
        [
            Line(1, DomainTransactionType.Sale, quantity: 3m, amount: 300m, cost: 120m),
            Line(1, DomainTransactionType.Sale, quantity: 1m, amount: 50m, cost: 20m),
            Line(2, DomainTransactionType.SaleRefund, quantity: 1m, amount: 100m, cost: 40m, estimated: true),
        ]);

        Assert.Equal((1, 1), (figures.Documents, figures.RefundDocuments));
        Assert.Equal(3m, figures.Quantity);
        Assert.Equal(250m, figures.NetAmount);
        Assert.Equal(100m, figures.Cost);
        Assert.Equal(150m, figures.GrossProfit);
        Assert.Equal(60m, figures.MarginPercent);
        Assert.True(figures.CostIsEstimated);
    }

    [Fact]
    public void Figures_MarginShouldBeNull_UnlessNetRevenueIsPositive()
    {
        Assert.Null(LineFigures.Of([]).MarginPercent);
        Assert.Null(LineFigures.Of([Line(9, DomainTransactionType.SaleRefund, 1m, 100m, 40m)]).MarginPercent);
    }

    private static ReportLine Line(
        int transactionId, DomainTransactionType type, decimal quantity, decimal amount, decimal cost, bool estimated = false) =>
        new(transactionId, type, DateTimeOffset.UnixEpoch, new DateOnly(2026, 10, 4), 1, 1, 1, 1, quantity, amount, cost, estimated);
}
