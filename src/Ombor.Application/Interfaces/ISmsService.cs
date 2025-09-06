using Ombor.Application.Models;

namespace Ombor.Application.Interfaces;

public interface ISmsService
{
    Task SendMessageAsync(SmsMessage message);
}
