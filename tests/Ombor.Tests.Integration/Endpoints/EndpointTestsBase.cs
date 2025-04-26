using System.Net.Http.Headers;
using AutoFixture;
using Bogus;
using Ombor.Application.Interfaces;
using Ombor.Tests.Integration.Helpers;
using Ombor.Tests.Integration.Helpers.ResponseValidators;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints;

[Collection(nameof(DatabaseCollection))]
public abstract class EndpointTestsBase : IClassFixture<TestingWebApplicationFactory>
{
    protected const string NotFoundTitle = "Not Found";

    protected readonly TestingWebApplicationFactory _factory;
    protected readonly ITestOutputHelper _outputHelper;
    protected readonly IApplicationDbContext _context;
    protected readonly ResponseValidator _responseValidator;
    protected readonly Fixture _fixture;
    protected readonly Faker _faker;
    protected readonly ApiClient _client;

    protected EndpointTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    {
        factory.ClientOptions.BaseAddress = new Uri("https://localhost/api/");
        factory.ClientOptions.AllowAutoRedirect = false;

        _factory = factory;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _outputHelper = outputHelper;
        _responseValidator = factory.ResponseValidator;
        _context = factory.Context;
        _fixture = CreateFixture();
        _faker = new Faker();
        _client = new ApiClient(client, outputHelper);
    }

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

    private static Fixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new NullRecursionBehavior());

        return fixture;
    }

    protected static class Routes
    {
        public const string Category = "categories";
        public const string Product = "products";
    }
}
