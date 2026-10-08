using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Domain.Exceptions;
using Ombor.Infrastructure.Services;

namespace Ombor.Tests.Unit.Services;

// Every Eskiz call is answered by a fake handler — no test ever reaches the real provider.
public sealed class SmsServiceTests
{
    private const string SendUrl = "https://sms.example.test/send";
    private const string AuthUrl = "https://sms.example.test/auth/login";

    private static readonly SmsMessage Message = new("+998900000000", "code 1234", "Ombor");

    private static SmsSettings StaticTokenSettings(string token = "static-token") => new()
    {
        Token = token,
        ApiUrl = SendUrl,
        FromNumber = "Ombor",
    };

    private static SmsSettings AccountSettings(string? fallbackToken = null) => new()
    {
        Token = fallbackToken,
        Email = "owner@example.test",
        Password = "secret",
        AuthUrl = AuthUrl,
        ApiUrl = SendUrl,
        FromNumber = "Ombor",
    };

    private static SmsService CreateService(FakeEskiz eskiz, SmsSettings settings)
    {
        var validator = new Mock<IRequestValidator>();
        validator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(settings);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(eskiz, disposeHandler: false));

        var tokens = new EskizTokenProvider(factory.Object, options, NullLogger<EskizTokenProvider>.Instance);

        return new SmsService(validator.Object, new HttpClient(eskiz, disposeHandler: false), tokens, options);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowSmsDeliveryException_WhenProviderRejects()
    {
        var eskiz = new FakeEskiz { SendStatus = _ => HttpStatusCode.InternalServerError };
        var service = CreateService(eskiz, StaticTokenSettings());

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowSmsDeliveryException_WhenProviderUnreachable()
    {
        var eskiz = new FakeEskiz { Unreachable = true };
        var service = CreateService(eskiz, StaticTokenSettings());

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldUseStaticToken_WhenNoAccountIsConfigured()
    {
        var eskiz = new FakeEskiz();
        var service = CreateService(eskiz, StaticTokenSettings("static-token"));

        await service.SendMessageAsync(Message);

        Assert.Equal(0, eskiz.LoginCount);
        Assert.Equal(new[] { "static-token" }, eskiz.SentWithTokens);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotRetry_WhenStaticTokenIsRejected()
    {
        var eskiz = new FakeEskiz { SendStatus = _ => HttpStatusCode.Unauthorized };
        var service = CreateService(eskiz, StaticTokenSettings());

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));

        Assert.Single(eskiz.SentWithTokens);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldLogInOnce_AndReuseTheToken()
    {
        var eskiz = new FakeEskiz();
        eskiz.IssueTokens(FakeEskiz.Jwt(expiresIn: TimeSpan.FromDays(30)));
        var service = CreateService(eskiz, AccountSettings());

        await service.SendMessageAsync(Message);
        await service.SendMessageAsync(Message);

        Assert.Equal(1, eskiz.LoginCount);
        Assert.Equal(2, eskiz.SentWithTokens.Count);
        Assert.All(eskiz.SentWithTokens, token => Assert.Equal(eskiz.Issued[0], token));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldRenewTokenAndResendOnce_WhenEskizAnswers401()
    {
        var first = FakeEskiz.Jwt(expiresIn: TimeSpan.FromDays(30));
        var second = FakeEskiz.Jwt(expiresIn: TimeSpan.FromDays(30));
        var eskiz = new FakeEskiz { SendStatus = token => token == first ? HttpStatusCode.Unauthorized : HttpStatusCode.OK };
        eskiz.IssueTokens(first, second);
        var service = CreateService(eskiz, AccountSettings());

        await service.SendMessageAsync(Message);

        Assert.Equal(2, eskiz.LoginCount);
        Assert.Equal(new[] { first, second }, eskiz.SentWithTokens);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldRenewProactively_WhenTokenExpiresWithinADay()
    {
        var expiring = FakeEskiz.Jwt(expiresIn: TimeSpan.FromHours(2));
        var fresh = FakeEskiz.Jwt(expiresIn: TimeSpan.FromDays(30));
        var eskiz = new FakeEskiz();
        eskiz.IssueTokens(expiring, fresh);
        var service = CreateService(eskiz, AccountSettings());

        await service.SendMessageAsync(Message);
        await service.SendMessageAsync(Message);

        Assert.Equal(2, eskiz.LoginCount);
        Assert.Equal(fresh, eskiz.SentWithTokens[^1]);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldFallBackToStaticToken_WhenLoginFails()
    {
        var eskiz = new FakeEskiz { LoginStatus = HttpStatusCode.Unauthorized };
        var service = CreateService(eskiz, AccountSettings(fallbackToken: "fallback-token"));

        await service.SendMessageAsync(Message);

        Assert.Equal(new[] { "fallback-token" }, eskiz.SentWithTokens);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowSmsDeliveryException_WhenLoginFailsWithoutFallback()
    {
        var eskiz = new FakeEskiz { LoginStatus = HttpStatusCode.InternalServerError };
        var service = CreateService(eskiz, AccountSettings());

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));

        Assert.Empty(eskiz.SentWithTokens);
    }

    [Fact]
    public void ReadExpiry_ShouldReadTheJwtExpClaim_AndIgnoreOpaqueTokens()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(30);

        Assert.Equal(expires.ToUnixTimeSeconds(), EskizTokenProvider.ReadExpiry(FakeEskiz.Jwt(expires))!.Value.ToUnixTimeSeconds());
        Assert.Null(EskizTokenProvider.ReadExpiry("not-a-jwt"));
    }

    [Fact]
    public void SmsSettings_ShouldRequireAnAccountOrAToken()
    {
        var neither = new SmsSettings { ApiUrl = SendUrl, FromNumber = "Ombor" };

        Assert.NotEmpty(neither.Validate(new ValidationContext(neither)));
        Assert.Empty(StaticTokenSettings().Validate(new ValidationContext(StaticTokenSettings())));
        Assert.Empty(AccountSettings().Validate(new ValidationContext(AccountSettings())));
    }

    /// <summary>Plays both Eskiz endpoints: login hands out the queued tokens, send records the bearer it got.</summary>
    private sealed class FakeEskiz : HttpMessageHandler
    {
        private readonly Queue<string> _tokens = new();

        public Func<string, HttpStatusCode> SendStatus { get; init; } = _ => HttpStatusCode.OK;
        public HttpStatusCode LoginStatus { get; init; } = HttpStatusCode.OK;
        public bool Unreachable { get; init; }
        public int LoginCount { get; private set; }
        public List<string> Issued { get; } = [];
        public List<string> SentWithTokens { get; } = [];

        public void IssueTokens(params string[] tokens)
        {
            foreach (var token in tokens)
            {
                _tokens.Enqueue(token);
            }
        }

        public static string Jwt(TimeSpan expiresIn) => Jwt(DateTimeOffset.UtcNow.Add(expiresIn));

        public static string Jwt(DateTimeOffset expires)
        {
            static string Encode(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return $"{Encode(new { alg = "HS256" })}.{Encode(new { exp = expires.ToUnixTimeSeconds(), nonce = Guid.NewGuid() })}.signature";
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Unreachable)
            {
                throw new HttpRequestException("network down");
            }

            if (request.RequestUri!.ToString() == AuthUrl)
            {
                LoginCount++;

                if (LoginStatus != HttpStatusCode.OK)
                {
                    return Task.FromResult(new HttpResponseMessage(LoginStatus));
                }

                var token = _tokens.Count > 0 ? _tokens.Dequeue() : Jwt(TimeSpan.FromDays(30));
                Issued.Add(token);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { message = "token_generated", data = new { token } })),
                });
            }

            var bearer = request.Headers.Authorization!.Parameter!;
            SentWithTokens.Add(bearer);

            return Task.FromResult(new HttpResponseMessage(SendStatus(bearer)) { Content = new StringContent("{}") });
        }
    }
}
