using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Tests.Integration.Helpers;

/// <summary>
/// A no-op SMS sender for the test host. The real <c>SmsService</c> is a live HTTP call to the provider, so any
/// test that exercises register/forgot-password would otherwise attempt a real send; this replaces that seam.
/// </summary>
internal sealed class FakeSmsService : ISmsService
{
    public Task SendMessageAsync(SmsMessage message) => Task.CompletedTask;
}
