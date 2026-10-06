using Ombor.Application.Models;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Sends an SMS in the background. Used where the response must not depend on the send — forgot-password answers
/// in the same time and with the same 200 whether or not the number has an account, and a provider failure is
/// logged instead of surfacing as a 503 that only a real account could trigger.
/// </summary>
public interface ISmsQueue
{
    void Enqueue(SmsMessage message);
}
