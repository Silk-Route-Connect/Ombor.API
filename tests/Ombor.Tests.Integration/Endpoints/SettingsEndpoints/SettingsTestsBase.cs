using System.Net;
using System.Net.Http.Headers;
using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Responses.Organization;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SettingsEndpoints;

public abstract class SettingsTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Settings;
    protected override string GetUrl(int id) => $"{Routes.Settings}/{id}";

    protected string OrganizationUrl => $"{Routes.Settings}/organization";

    protected Task<OrganizationProfileDto> GetOrganizationAsync() =>
        _client.GetAsync<OrganizationProfileDto>(OrganizationUrl);

    protected Task<OrganizationProfileDto> UpdateOrganizationAsync(
        string name, string? address = null, string? phone = null, string? email = null, bool withLogo = false) =>
        _client.PutAsync<OrganizationProfileDto>(
            OrganizationUrl, BuildOrganizationForm(name, address, phone, email, withLogo), HttpStatusCode.OK);

    protected static MultipartFormDataContent BuildOrganizationForm(
        string name, string? address, string? phone, string? email, bool withLogo)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(name), nameof(UpdateOrganizationRequest.Name) },
        };

        if (address is not null)
        {
            form.Add(new StringContent(address), nameof(UpdateOrganizationRequest.Address));
        }

        if (phone is not null)
        {
            form.Add(new StringContent(phone), nameof(UpdateOrganizationRequest.Phone));
        }

        if (email is not null)
        {
            form.Add(new StringContent(email), nameof(UpdateOrganizationRequest.Email));
        }

        if (withLogo)
        {
            // Minimal PNG header bytes — the original is stored as-is; the thumbnailer fails silently on
            // non-image content, which is fine (we only need a returned logo URL).
            var logo = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
            logo.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            form.Add(logo, nameof(UpdateOrganizationRequest.Logo), "logo.png");
        }

        return form;
    }
}
