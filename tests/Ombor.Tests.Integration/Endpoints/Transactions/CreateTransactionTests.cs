using System.Net;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper output) : TransactionsTestsBase(factory, output)
{
    private async Task<TransactionDto> PostTransactionAsync(CreateTransactionRequest request)
    {
        var form = request.ToMultipartFormData();
        return await _client.PostAsync<TransactionDto>(Routes.Transaction, form, HttpStatusCode.Created);
    }
}
