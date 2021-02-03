using Mailer.Domain.Application;
using Mailer.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Api.Background
{
    public class MailingSubscriber : BackgroundService
    {
        private readonly IMessageBus<ExecuteMailingMessage> _messageBus;
        private readonly IServiceScope _serviceScope;
        private readonly IMailingService _mailingService;

        public MailingSubscriber(IMessageBus<ExecuteMailingMessage> messageBus, IServiceScopeFactory serviceScopeFactory)
        {
            _messageBus = messageBus;
            _serviceScope = serviceScopeFactory.CreateScope();
            _mailingService = _serviceScope.ServiceProvider.GetRequiredService<IMailingService>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _messageBus.ConsumeStartAsync(async msg =>
            {
                var context = new MailerContext(msg.UserId, stoppingToken);
                var model = await _mailingService.GetAsync(context, msg.MailingId);
                if (model is null) return;
                await _mailingService.ProcessAsync(context, model);
            });
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _messageBus.ConsumeStopAsync();
            }
            catch (InvalidOperationException) { }
        }

        public override void Dispose()
        {
            _serviceScope?.Dispose();
            base.Dispose();
        }
    }
}
