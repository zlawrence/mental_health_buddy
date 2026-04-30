using System.Threading.Channels;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;

namespace MentalHealthApp.Infrastructure.Services;

// Singleton: acts as IEmailQueue for producers and exposes the ChannelReader for the background processor.
public sealed class EmailQueueChannel : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken);
}
