using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TransactionEndpoints;

public class CreateTransactionTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TransactionTestsBase(factory, outputHelper)
{
    public static TheoryData<TransactionTestCase> EqualPaymentTransactions => new()
    {
        new TransactionTestCase(100, 100, 0, 0, TransactionStatus.Closed),
        new TransactionTestCase(100, 100, -10, -10, TransactionStatus.Closed),
        new TransactionTestCase(100, 100, 10, 10, TransactionStatus.Closed),
    };

    public static TheoryData<TransactionTestCase> SupplyOverpaymennts => new()
    {
        new TransactionTestCase(100, 150, 0, -50, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, -40, -90, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, -50, 100, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, -60, -110, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, 40, -10, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, 50, 0, TransactionStatus.Closed),
        new TransactionTestCase(100, 150, 60, 10, TransactionStatus.Closed),
    };

    [Fact]
    public async Task CreateAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange

        // Act

        // Assert
    }

    [Theory]
    [MemberData(nameof(EqualPaymentTransactions))]
    [MemberData(nameof(SupplyOverpaymennts))]
    public async Task CreateAsync_ShouldCorrectlyHandleTransactionAndPayment(TransactionTestCase testCase)
    {
        // Arrange
        var partner = _builder.PartnerBuilder
            .WithName($"Partner for Supply test.{Guid.NewGuid()}")
            .WithType(PartnerType.All)
            .WithBalance(testCase.PartnerBalanceBefore)
            .Build();
        await CreatePartnerAsync(partner);
        var request = TransactionRequestFactory.GenerateValidCreateRequest(
            partner.Id,
            Contracts.Enums.TransactionType.Supply,
            testCase.TransactionTotalPaid,
            testCase.TransactionTotalDue);
        var formData = request.ToMultipartFormData();

        // Act
        var response = await _client.PostAsync<CreateTransactionResponse>(GetUrl(), formData);

        // Assert
        await _responseValidator.Transaction.ValidatePostAsync(request, response);
        await _responseValidator.Transaction.ValidatePartnerAsync(request, partner);
        await _responseValidator.Transaction.ValidatePaymentAsync(request, response, partner);
    }

    public sealed record TransactionTestCase(
        int TransactionTotalDue,
        int TransactionTotalPaid,
        decimal PartnerBalanceBefore,
        decimal PartnerBalanceAfter,
        TransactionStatus ExpectedStatus);
}
