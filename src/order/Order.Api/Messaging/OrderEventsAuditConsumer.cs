using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Order.Api.Messaging;

public sealed class OrderEventsAuditConsumer : BackgroundService
{
    private readonly IModel _channel;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<OrderEventsAuditConsumer> _log;

    public OrderEventsAuditConsumer(IModel channel, IOptions<RabbitMqOptions> opt, ILogger<OrderEventsAuditConsumer> log)
    {
        _channel = channel;
        _opt = opt.Value;
        _log = log;

        _channel.ExchangeDeclare(_opt.Exchange, "topic", durable: true, autoDelete: false);
        _channel.QueueDeclare(_opt.AuditQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_opt.AuditQueue, _opt.Exchange, routingKey: _opt.RoutingFulfilled);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
            _log.LogInformation("Audit consume: routing={RoutingKey}, payload={Payload}", ea.RoutingKey, msg);

            await Task.CompletedTask;
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: _opt.AuditQueue, autoAck: false, consumer: consumer);
        _log.LogInformation("OrderEventsAuditConsumer started, queue={Queue}", _opt.AuditQueue);
        return Task.CompletedTask;
    }
}