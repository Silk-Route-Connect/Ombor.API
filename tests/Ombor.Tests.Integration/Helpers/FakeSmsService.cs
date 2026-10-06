using System.Collections.Concurrent;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Tests.Integration.Helpers;

/// <summary>
/// A recording SMS sender for the test host. The real <c>SmsService</c> is a live HTTP call to the provider, so any
/// test that exercises register/forgot-password would otherwise attempt a real send; this replaces that seam and keeps
/// what would have been sent, so tests can assert the text.
/// </summary>
internal sealed class FakeSmsService : ISmsService
{
    // Static: the sender is scoped (and the reset SMS is sent from the background queue's own scope), and every test
    // uses a fresh phone number, so filtering by number keeps tests apart.
    private static readonly ConcurrentQueue<SmsMessage> Sent = new();

    public Task SendMessageAsync(SmsMessage message)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The messages sent to one number so far, oldest first.</summary>
    public static IReadOnlyList<SmsMessage> SentTo(string phoneNumber) =>
        Sent.Where(m => m.ToNumber == phoneNumber).ToList();
}
