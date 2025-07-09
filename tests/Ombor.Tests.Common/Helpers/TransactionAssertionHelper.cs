using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class TransactionAssertionHelper
{
    /// <summary>
    /// Asserts that the specified <see cref="CreateTransactionRequest"/> and <see cref="CreateTransactionResponse"/>
    /// are equivalent based on their properties and calculated values.
    /// </summary>
    /// <remarks>
    /// This method compares the key properties of the <paramref name="expected"/> request and the
    /// <paramref name="actual"/> response, including calculated values such as the total due and transaction status. It
    /// ensures that the response matches the expected request in terms of partner ID, type, notes, and line items and payment amount.
    /// </remarks>
    /// <param name="expected">The expected <see cref="CreateTransactionRequest"/> object containing the original transaction details.</param>
    /// <param name="actual">The actual <see cref="CreateTransactionResponse"/> object to be validated against the expected request.</param>
    public static void AssertEquivalent(CreateTransactionRequest expected, CreateTransactionResponse actual)
    {
        var expectedTotalDue = expected.Lines.Sum(x => (x.Quantity * x.UnitPrice) - x.Discount);
        var expectedStatus = expected.TotalPaid >= expectedTotalDue
            ? Contracts.Enums.TransactionStatus.Closed
            : Contracts.Enums.TransactionStatus.Open;
        var expectedTotalPaid = expected.TotalPaid > expectedTotalDue
            ? expectedTotalDue
            : expected.TotalPaid;

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expectedTotalPaid, actual.TotalPaid);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expectedTotalDue, actual.TotalDue);
        Assert.Equal(expectedStatus, actual.Status);

        AssertEquivalent(expected.Lines, actual.Lines);
    }

    /// <summary>
    /// Asserts that the specified <see cref="CreateTransactionRequest"/> object is equivalent to the given
    /// <see cref="TransactionRecord"/> object.
    /// </summary>
    /// <remarks>
    /// This method compares the properties of the <paramref name="expected"/> and <paramref name="actual"/>
    /// objects to ensure they match. It also validates that the calculated total due and transaction
    /// status, total paid in the <paramref name="expected"/> object align with the corresponding values in the
    /// <paramref name="actual"/> object. Nested transaction lines are recursively compared for equivalence.
    /// </remarks>
    /// <param name="expected">The expected transaction request containing the original data.</param>
    /// <param name="actual">The actual transaction record to compare against the expected request.</param>
    public static void AssertEquivalent(CreateTransactionRequest expected, TransactionRecord actual)
    {
        var expectedTotalDue = expected.CalculateTotalDue();
        var expectedStatus = expected.TotalPaid >= expectedTotalDue
            ? Contracts.Enums.TransactionStatus.Closed
            : Contracts.Enums.TransactionStatus.Open;
        var expectedTotalPaid = expected.TotalPaid > expectedTotalDue
            ? expectedTotalDue
            : expected.TotalPaid;

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expectedTotalPaid, actual.TotalPaid);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expectedTotalDue, actual.TotalDue);
        Assert.Equal(expectedStatus.ToString(), actual.Status.ToString());

        AssertEquivalent(expected.Lines, actual.Lines);
    }

    private static void AssertEquivalent(IEnumerable<CreateTransactionLine> expectedLines, IEnumerable<TransactionLineDto> actualLines)
    {
        foreach (var expectedLine in expectedLines)
        {
            var actualLine = actualLines.FirstOrDefault(x => x.ProductId == expectedLine.ProductId);

            Assert.NotNull(actualLine);
            Assert.False(string.IsNullOrWhiteSpace(actualLine.ProductName));

            Assert.Equal(expectedLine.Quantity, actualLine.Quantity);
            Assert.Equal(expectedLine.UnitPrice, actualLine.UnitPrice);
            Assert.Equal(expectedLine.Discount, actualLine.Discount);
        }
    }

    private static void AssertEquivalent(IEnumerable<CreateTransactionLine> expectedLines, IEnumerable<TransactionLine> actualLines)
    {
        foreach (var expectedLine in expectedLines)
        {
            var actualLine = actualLines.FirstOrDefault(x => x.ProductId == expectedLine.ProductId);

            Assert.NotNull(actualLine);

            Assert.Equal(expectedLine.Quantity, actualLine.Quantity);
            Assert.Equal(expectedLine.UnitPrice, actualLine.UnitPrice);
            Assert.Equal(expectedLine.Discount, actualLine.Discount);
        }
    }
}
