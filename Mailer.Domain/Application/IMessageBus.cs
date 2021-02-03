using System;
using System.Threading.Tasks;

namespace Mailer.Domain.Application
{
    public interface IMessage {
        public Guid UserId { get; }
    }

    public interface IMessageBus<TMessage> where TMessage : IMessage
    {
        Task PublishAsync(TMessage message);
        Task ConsumeStartAsync(Func<TMessage, Task> handleMessage);
        Task ConsumeStopAsync();
    }
}
