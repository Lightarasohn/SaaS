using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SaaS.Emails
{
    public sealed record EmailJob(string ToEmail, string Subject, string Message, bool IsHtml = true);

    public interface IEmailQueue
    {
        ValueTask EnqueueAsync(EmailJob emailJob, CancellationToken cancellationToken);
        IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken cancellationToken);
    }

    public sealed class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailJob> _channel = Channel.CreateBounded<EmailJob>(
            new BoundedChannelOptions(1000)
            { 
                FullMode = BoundedChannelFullMode.Wait 
            });
        public ValueTask EnqueueAsync(EmailJob emailJob, CancellationToken cancellationToken)
        {
            return _channel.Writer.WriteAsync(emailJob, cancellationToken);
        }

        public IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}