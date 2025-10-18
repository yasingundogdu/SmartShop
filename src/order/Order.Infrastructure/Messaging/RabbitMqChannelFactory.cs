using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using IModel = RabbitMQ.Client.IModel;

namespace Order.Infrastructure.Messaging;

public static class RabbitMqChannelFactory
{
    public static IModel CreateModel(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "rabbitmq",
            Port = int.TryParse(config["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Pass"] ?? "guest",
            VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
            DispatchConsumersAsync = true
        };

        var connection = factory.CreateConnection();
        return connection.CreateModel();
    }
}