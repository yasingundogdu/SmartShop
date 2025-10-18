using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Application.Abstractions.Messaging;
using RabbitMQ.Client;

namespace Order.Api.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IModel _channel;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<RabbitMqEventPublisher> _log;

    public RabbitMqEventPublisher(IModel channel, IOptions<RabbitMqOptions> opt, ILogger<RabbitMqEventPublisher> log)
    {
        _channel = channel;
        _opt = opt.Value;
        _log = log;

        _channel.ExchangeDeclare(exchange: _opt.Exchange, type: "topic", durable: true, autoDelete: false);
    }

    public Task PublishOrderFulfilledAsync(Order.Application.Orders.Events.OrderFulfilledEvent evt, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        _channel.BasicPublish(exchange: _opt.Exchange,
            routingKey: _opt.RoutingFulfilled,
            basicProperties: props,
            body: body);

        _log.LogInformation("Published OrderFulfilled event: {OrderId}", evt.OrderId);
        return Task.CompletedTask;
    }
    
    public Task PublishRawAsync(string exchange, string routingKey, string bodyUtf8, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(bodyUtf8);
        _channel.BasicPublish(exchange, routingKey, null, body);
        return Task.CompletedTask;
    }
}