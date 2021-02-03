using Mailer.Domain.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mailer.Infrastructure.Connectors
{
    public class RabbitMqOptions
    {
        public bool ConsumerOn { get; set; }
        public string HostName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string QueueName { get; set; } = default!;
    }

    public class RabbitMqMessageBus<TMessage> : IMessageBus<TMessage>, IDisposable
        where TMessage : IMessage
    {
        private IConnection? _consumerConnection;
        private IModel? _consumerChannel;
        private string? _consumerTag;
        private readonly object _consumerLock = new object();

        private IConnection? _publisherConnection;

        private readonly IPolicyProvider _policyProvider;
        private readonly ILogger<RabbitMqMessageBus<TMessage>> _logger;
        private readonly RabbitMqOptions _options;

        public RabbitMqMessageBus(IPolicyProvider policyProvider, ILogger<RabbitMqMessageBus<TMessage>> logger, IOptions<RabbitMqOptions> optionsAccessor)
        {
            _policyProvider = policyProvider;
            _logger = logger;
            _options = optionsAccessor.Value;
        }

        public void Initialize()
        {
            _policyProvider.Get<object?>("rabbitmq.initialize",
                new PolicyConfig { Wr = new PolicyConfig.WrConfig(5, TimeSpan.FromMilliseconds(300)) })
                .ExecuteAsync(() =>
                {
                    var connectionFactory = new ConnectionFactory {
                        HostName = _options.HostName,
                        UserName = _options.UserName,
                        Password = _options.Password,
                        DispatchConsumersAsync = true
                    };
                    if (_options.ConsumerOn)
                    {
                        _consumerConnection = connectionFactory.CreateConnection();
                        _consumerChannel = _consumerConnection.CreateModel();

                        _consumerChannel.QueueDeclare(_options.QueueName, true, false, false);
                        _consumerChannel.CallbackException += (chann, args) =>
                        {
                            _logger.LogError(args.Exception, "Error for channel: {channel}", chann);
                        };
                    }
                    _publisherConnection = connectionFactory.CreateConnection();
                    return Task.FromResult(default(object));
                }).GetAwaiter().GetResult();
        }

        public async Task PublishAsync(TMessage message)
        {
            if (_publisherConnection is null)
                throw new InvalidOperationException($"Published connection not initialized. Call {nameof(Initialize)} first.");


            await _policyProvider.Get<object?>("rabbitmq.publish",
                new PolicyConfig {
                    Wr = new PolicyConfig.WrConfig(5, TimeSpan.FromMilliseconds(300)),
                    Cb = new PolicyConfig.CbConfig(10, TimeSpan.FromSeconds(2))
                })
                .ExecuteAsync(() =>
                {
                    using var channel = _publisherConnection.CreateModel();
                    channel.QueueDeclare(_options.QueueName, true, false, false);

                    var body = JsonSerializer.SerializeToUtf8Bytes(message);
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    channel.BasicPublish("", _options.QueueName, properties, body);
                    return Task.FromResult(default(object));
                });
        }

        public Task ConsumeStartAsync(Func<TMessage, Task> handleMessage)
        {
            if (_consumerChannel is null)
                throw new InvalidOperationException($"Consumer channel not initialized. Call {nameof(Initialize)} first.");

            lock (_consumerLock)
            {
                if (_consumerTag != null)
                    throw new InvalidOperationException("Consumer already started");

                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    TMessage message;
                    try
                    {
                        message = JsonSerializer.Deserialize<TMessage>(body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during message deserialization: {body}", body);
                        _consumerChannel.BasicReject(ea.DeliveryTag, false);
                        return;
                    }
                    try
                    {
                        await handleMessage(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during message processing: {message}", ex.Message);
                        _consumerChannel.BasicNack(ea.DeliveryTag, false, true);
                        return;
                    }
                    _consumerChannel.BasicAck(ea.DeliveryTag, false);
                };
                _consumerTag = _consumerChannel.BasicConsume(_options.QueueName, false, consumer);
            }
            return Task.CompletedTask;
        }

        public Task ConsumeStopAsync()
        {
            lock (_consumerLock)
            {
                if (_consumerTag is null)
                    throw new InvalidOperationException("Consumer not started");
                _consumerChannel!.BasicCancel(_consumerTag);
                _consumerTag = null;
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _consumerConnection?.Dispose();
            _publisherConnection?.Dispose();
        }
    }
}
