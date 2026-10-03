using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Services;

/// <summary>
/// In-process SMS queue drained by a hosted worker through the registered <see cref="ISmsService"/>. Messages still
/// queued at shutdown are lost — acceptable for one-time codes, which the user simply requests again.
/// </summary>
internal sealed class BackgroundSmsQueue(IServiceScopeFactory scopeFactory, ILogger<BackgroundSmsQueue> logger)
    : BackgroundService, ISmsQueue
{
    // Bounded so a flood of requests cannot grow memory without limit; the throttles keep real traffic far below it.
    private readonly Channel<SmsMessage> _channel = Channel.CreateBounded<SmsMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Enqueue(SmsMessage message)
    {
        if (!_channel.Writer.TryWrite(message))
        {
            logger.LogError("SMS queue is full; a message to {ToNumber} was dropped.", message.ToNumber);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

                await smsService.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queued SMS to {ToNumber} failed.", message.ToNumber);
            }
        }
    }
}
