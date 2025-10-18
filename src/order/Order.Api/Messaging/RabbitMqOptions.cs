namespace Order.Api.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "order.events";
    public string RoutingFulfilled { get; set; } = "order.fulfilled";
    public string AuditQueue { get; set; } = "order.events.audit";
}