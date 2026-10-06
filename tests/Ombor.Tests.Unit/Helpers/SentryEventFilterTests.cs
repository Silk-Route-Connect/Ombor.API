using FluentValidation;
using Ombor.API.Extensions;
using Ombor.Domain.Exceptions;
using Sentry;

namespace Ombor.Tests.Unit.Helpers;

public sealed class SentryEventFilterTests
{
    [Fact]
    public void BeforeSend_DropsTheWholeBody_OnAuthRoutes()
    {
        var @event = EventFor("https://api.ombor.uz/api/auth/register", """{"phoneNumber":"+998901234567","password":"Secret123"}""");

        var sent = SentryEventFilter.BeforeSend(@event, new SentryHint());

        Assert.NotNull(sent);
        Assert.Null(sent.Request.Data);
    }

    [Fact]
    public void BeforeSend_DropsTheWholeBody_OnChangePassword()
    {
        var @event = EventFor("https://api.ombor.uz/api/settings/password", """{"currentPassword":"a","newPassword":"b"}""");

        Assert.Null(SentryEventFilter.BeforeSend(@event, new SentryHint())!.Request.Data);
    }

    [Fact]
    public void BeforeSend_MasksSecretFields_InOtherBodies_AtAnyDepth()
    {
        var @event = EventFor(
            "https://api.ombor.uz/api/partners",
            """{"name":"Acme","password":"p1","nested":{"ConfirmPassword":"p2","items":[{"refreshToken":"t"}]}}""");

        var data = (string)SentryEventFilter.BeforeSend(@event, new SentryHint())!.Request.Data!;

        Assert.Contains("Acme", data);
        Assert.DoesNotContain("p1", data);
        Assert.DoesNotContain("p2", data);
        Assert.DoesNotContain("\"t\"", data);
        Assert.Contains("[Filtered]", data);
    }

    [Fact]
    public void BeforeSend_FiltersNonJsonBodies_ThatMentionASecret()
    {
        var @event = EventFor("https://api.ombor.uz/api/partners", "password=p1&name=Acme");

        Assert.Equal("[Filtered]", SentryEventFilter.BeforeSend(@event, new SentryHint())!.Request.Data);
    }

    [Fact]
    public void BeforeSend_DropsClientErrors()
    {
        Assert.Null(SentryEventFilter.BeforeSend(new SentryEvent(new ValidationException("bad")), new SentryHint()));
        Assert.Null(SentryEventFilter.BeforeSend(new SentryEvent(new TooManyRequestsException(TimeSpan.FromSeconds(5))), new SentryHint()));
        Assert.Null(SentryEventFilter.BeforeSend(
            new SentryEvent(new AuthenticationFailedException(ErrorCodes.InvalidCredentials, "no")), new SentryHint()));
    }

    [Fact]
    public void BeforeSend_KeepsServerErrors()
    {
        Assert.NotNull(SentryEventFilter.BeforeSend(new SentryEvent(new InvalidOperationException("boom")), new SentryHint()));
    }

    private static SentryEvent EventFor(string url, string body)
    {
        var @event = new SentryEvent(new InvalidOperationException("boom"));
        @event.Request.Url = url;
        @event.Request.Data = body;

        return @event;
    }
}
