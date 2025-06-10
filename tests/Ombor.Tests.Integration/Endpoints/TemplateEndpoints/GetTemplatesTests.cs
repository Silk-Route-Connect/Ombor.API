using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public sealed class GetTemplatesTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    // Keep search terms longer than 4 characters
    // to create templates matching only part of the search.
    public static TheoryData<GetTemplatesRequest> Requests =>
        new()
        {
            new GetTemplatesRequest(),
            new GetTemplatesRequest("   "),
            new GetTemplatesRequest("Test Template"),
            new GetTemplatesRequest(null, TemplateType.Sale),
            new GetTemplatesRequest("Test Template", TemplateType.Supply)
        };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task GetAsync_ShouldReturnMatchingTemplates(GetTemplatesRequest request)
    {
        // Arrange
        await CreateTemplatesAsync(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<TemplateDto[]>(url);

        // Assert
        await _responseValidator.Template.ValidateGetAsync(request, response);
    }

    private async Task CreateTemplatesAsync(GetTemplatesRequest request)
    {
        var matchingTemplates = CreateMatchinTemplates(request);
        var nonMatchingTemplates = CreateNonMatchingTemplates();
        Template[] templates = [.. matchingTemplates, .. nonMatchingTemplates];

        await CreateAsync(templates);
    }

    private static Template[] CreateMatchinTemplates(GetTemplatesRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var templates = new List<Template>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm) && request.Type.HasValue)
        {
            // Template matching type and part of the search
            templates.Add(new Template
            {
                Name = request.SearchTerm[..4],
                Type = Enum.Parse<Domain.Enums.TemplateType>(request.Type.Value.ToString()),
            });

            // Template matching type and part of the search
            templates.Add(new Template
            {
                Name = request.SearchTerm[4..],
            });
        }

        // This template will only match search term, if request contains type as well,
        // it will not match full query filter.
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            templates.Add(new Template
            {
                Name = request.SearchTerm[..4],
                Type = Domain.Enums.TemplateType.Sale,
            });
        }

        // This template will only match type, if request contains search term as well,
        // it will not match full query filter.
        if (request.Type.HasValue)
        {
            templates.Add(new Template
            {
                Name = $"Template matching type: {request.Type}",
                Type = Enum.Parse<Domain.Enums.TemplateType>(request.Type.Value.ToString()),
            });
        }

        return [.. templates];
    }

    private static Template[] CreateNonMatchingTemplates() =>
        Enumerable.Range(0, 5)
        .Select(i => new Template
        {
            Name = $"Template-{i.ToString()}",
            Type = Domain.Enums.TemplateType.Supply
        })
        .ToArray();
}
