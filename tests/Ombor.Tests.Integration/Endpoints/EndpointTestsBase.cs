using System.Net.Http.Headers;
using Bogus;
using Ombor.Application.Interfaces;
using Ombor.Tests.Integration.Helpers;
using Ombor.Tests.Integration.Helpers.ResponseValidators;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints;

[Collection(nameof(DatabaseCollection))]
public abstract class EndpointTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : IClassFixture<TestingWebApplicationFactory>
{
    protected const string NotFoundTitle = "Not Found";

    protected readonly ITestOutputHelper _outputHelper = outputHelper;
    protected readonly IApplicationDbContext _context = factory.Context;
    protected readonly ResponseValidator _responseValidator = factory.ResponseValidator;
    protected readonly Faker _faker = new();
    protected readonly ApiClient _client = CreateApiClient(factory, outputHelper);

    public static readonly IEnumerable<object[]> InvalidIds = [[-10], [-1], [0]];

    protected abstract string GetUrl();

    protected abstract string GetUrl(int id);

    protected string GetUrl<TRequest>(TRequest request)
    {
        var url = GetUrl();

        if (request is not null)
        {
            var queryParameters = request.GetType()
                .GetProperties()
                .Select(p => $"{p.Name}={p.GetValue(request)}");
            var queryString = string.Join("&", queryParameters);

            return $"{url}?{queryString}";
        }

        return url;
    }

    protected static string GetNotFoundErrorMessage(int id, string typeName)
        => $"{typeName} with ID {id} was not found.";

    private static ApiClient CreateApiClient(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    {
        factory.ClientOptions.BaseAddress = new Uri("https://localhost/api/");
        factory.ClientOptions.AllowAutoRedirect = false;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return new ApiClient(client, outputHelper);
    }

    protected static class Routes
    {
        public const string Category = "categories";
        public const string Product = "products";
    }
}
