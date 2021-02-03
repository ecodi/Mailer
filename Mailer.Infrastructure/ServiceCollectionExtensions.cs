using Mailer.Domain.Application;
using Mailer.Domain.Repositories;
using Mailer.Domain.Services;
using Mailer.Infrastructure.Connectors;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Mailer.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMailingMessageBus(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
        {
            services.AddSingleton<IMessageBus<ExecuteMailingMessage>, RabbitMqMessageBus<ExecuteMailingMessage>>(s =>
            {
                var bus = new RabbitMqMessageBus<ExecuteMailingMessage>(s.GetRequiredService<IPolicyProvider>(),
                    s.GetRequiredService<ILogger<RabbitMqMessageBus<ExecuteMailingMessage>>>(),
                    Options.Create(GetOptions())
                );
                bus.Initialize();
                return bus;
            });

            return services;

            RabbitMqOptions GetOptions()
            {
                var options = new RabbitMqOptions();
                configureOptions(options);
                return options;
            }
        }

        public static IServiceCollection AddMongoDb(this IServiceCollection services, Action<MongoOptions> configureOptions)
            => services
                .AddScoped<IMongoContext, MongoContext>()
                .AddScoped<IRecipientRepository, IMongoRepository<RecipientDbModel>, IMongoRepository, RecipientRepository>()
                .AddScoped<ISenderRepository, IMongoRepository<SenderDbModel>, IMongoRepository, SenderRepository>()
                .AddScoped<IMailingRepository, IMongoRepository<MailingDbModel>, IMongoRepository, MailingRepository>()
                .AddScoped<IEmailRepository, IMongoRepository<EmailDbModel>, IMongoRepository, EmailRepository>()
                .AddOptions()
                    .Configure(configureOptions);

        public static IServiceCollection AddMailClient(this IServiceCollection services, Action<SendGridOptions> configureOptions)
            => services
                .AddScoped<IMailClient>(s =>
                {
                    var client = new SendGridMailClient(s.GetRequiredService<IPolicyProvider>(),
                        s.GetRequiredService<ILogger<SendGridMailClient>>(),
                        s.GetRequiredService<IOptions<SendGridOptions>>());
                    client.Initialize();
                    return client;
                })
                .AddOptions()
                    .Configure(configureOptions);
    }
}
