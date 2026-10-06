using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Organization;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Organization;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.Uploads;

/// <summary>
/// backend-10 / scope-17: attachments and organization logos are served at the URL the API returns (like product
/// images), under a random file name. backend-21: uploads are checked by content and served with <c>nosniff</c>.
/// </summary>
public sealed partial class UploadServingTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    private const string OrganizationUrl = "settings/organization";

    protected override string GetUrl() => "payments";

    protected override string GetUrl(int id) => $"payments/{id}";

    [Fact]
    public async Task PaymentAttachment_ShouldBeDownloadableAtItsUrl_WithTheDetectedType()
    {
        var walletId = await CreateWalletAsync();
        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Income, null, null, walletId,
            Amount: 1_000m, Description: "receipt", Period: null, Settlements: [],
            Attachments: [BuildFile("receipt.pdf", "application/octet-stream", TestFiles.Pdf)]);

        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());

        var attachment = Assert.Single(payment.Attachments);
        // The stored type comes from the bytes, not from the client's claim.
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Matches(RandomFileName(), attachment.Url);

        using var response = await DownloadAsync(attachment.Url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(TestFiles.Pdf, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Attachment_ShouldBeRejected_WhenItsBytesAreNotWhatItsNameClaims()
    {
        var walletId = await CreateWalletAsync();
        var request = new CreatePaymentRecordRequest(
            PaymentType.General, PaymentDirection.Income, null, null, walletId,
            Amount: 1_000m, Description: "receipt", Period: null, Settlements: [],
            Attachments: [BuildFile("receipt.jpg", "image/jpeg", "<html><script>alert(1)</script></html>"u8.ToArray())]);

        var problem = await _client.PostAsync<JObject>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Equal("file.invalid", (string?)problem["code"]);
    }

    [Fact]
    public async Task Logo_ShouldBeServed_AndRemovable()
    {
        var withLogo = await PutOrganizationAsync(logo: ("logo.png", TestFiles.Png));

        Assert.NotNull(withLogo.LogoUrl);
        using (var response = await DownloadAsync(withLogo.LogoUrl!))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        }

        var removed = await PutOrganizationAsync(removeLogo: true);

        Assert.Null(removed.LogoUrl);
    }

    [Fact]
    public async Task Logo_ShouldRejectAPdf_BecauseLogosAreImagesOnly()
    {
        using var form = OrganizationForm(logo: ("logo.pdf", TestFiles.Pdf), removeLogo: false);

        var problem = await _client.PutAsync<JObject>(OrganizationUrl, form, HttpStatusCode.BadRequest);

        Assert.Equal("file.invalid", (string?)problem["code"]);
    }

    private async Task<HttpResponseMessage> DownloadAsync(string relativeUrl)
    {
        var client = _factory.CreateClient();

        return await client.GetAsync($"/{relativeUrl.TrimStart('/')}");
    }

    private async Task<OrganizationProfileDto> PutOrganizationAsync((string Name, byte[] Bytes)? logo = null, bool removeLogo = false)
    {
        using var form = OrganizationForm(logo, removeLogo);

        return await _client.PutAsync<OrganizationProfileDto>(OrganizationUrl, form);
    }

    private static MultipartFormDataContent OrganizationForm((string Name, byte[] Bytes)? logo, bool removeLogo)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent($"Acme {Guid.NewGuid():N}"), nameof(UpdateOrganizationRequest.Name) },
            { new StringContent(removeLogo ? "true" : "false"), nameof(UpdateOrganizationRequest.RemoveLogo) },
        };

        if (logo is { } file)
        {
            var content = new ByteArrayContent(file.Bytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(content, nameof(UpdateOrganizationRequest.Logo), file.Name);
        }

        return form;
    }

    private async Task<int> CreateWalletAsync()
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = DomainEnums.WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    private static FormFile BuildFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);

        return new FormFile(stream, 0, content.Length, "Attachments", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [GeneratedRegex("/[0-9a-f]{32}\\.[a-z]+$")]
    private static partial Regex RandomFileName();
}
