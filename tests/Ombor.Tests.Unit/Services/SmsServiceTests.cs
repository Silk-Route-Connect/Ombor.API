using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Domain.Exceptions;
using Ombor.Infrastructure.Services;

namespace Ombor.Tests.Unit.Services;

public sealed class SmsServiceTests
{
    private static readonly SmsMessage Message = new("+998900000000", "code 1234", "Ombor");

    private static SmsService CreateService(HttpMessageHandler handler)
    {
        var validator = new Mock<IRequestValidator>();
        validator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var settings = Options.Create(new SmsSettings
        {
            Token = "token",
            ApiUrl = "https://sms.example.test/send",
            FromNumber = "Ombor",
        });

        return new SmsService(validator.Object, new HttpClient(handler), settings);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowSmsDeliveryException_WhenProviderRejects()
    {
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("provider error"),
        }));

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowSmsDeliveryException_WhenProviderUnreachable()
    {
        var service = CreateService(new ThrowingHandler());

        await Assert.ThrowsAsync<SmsDeliveryException>(() => service.SendMessageAsync(Message));
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSucceed_WhenProviderAccepts()
    {
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        await service.SendMessageAsync(Message);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("network down");
    }
}
