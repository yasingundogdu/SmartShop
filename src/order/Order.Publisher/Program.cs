using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Api.Messaging;
using Order.Application.Abstractions.Db;
using Order.Application.Abstractions.Messaging;
using Order.Infrastructure.Messaging;     
using Order.Infrastructure.Persistence;    
using Order.Publisher.Workers;              
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
var config   = builder.Configuration;

var connString =
    config.GetConnectionString("Postgres")
    ?? config["ConnectionStrings:Postgres"]
    ?? "Host=postgres;Port=5432;Database=orderdb;Username=postgres;Password=postgres";

services.Configure<RabbitMqOptions>(config.GetSection("RabbitMQ"));

services.AddDbContext<OrderDbContext>(opt =>
    opt.UseNpgsql(connString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "order")));

services.AddScoped<IOrderDbContext, OrderDbContext>();

services.AddSingleton<IModel>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return RabbitMqChannelFactory.CreateModel(cfg);
});

services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
services.AddHostedService<OutboxPublisherWorker>();
services.AddLogging(b => b.AddConsole());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

await app.RunAsync();