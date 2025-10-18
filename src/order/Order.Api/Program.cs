using System.Net;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Order.Api.Messaging;                 
using Order.Application.Abstractions.Db;    
using Order.Application.Abstractions.Messaging;
using Order.Application.Orders.Queries;      
using Order.Infrastructure.Persistence;       
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var conn =
    builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["ConnectionStrings:Postgres"]
    ?? "Host=postgres;Port=5432;Database=orderdb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "order")));

builder.Services.AddScoped<IOrderDbContext>(sp => sp.GetRequiredService<OrderDbContext>());

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetFulfilledOrdersQuery).Assembly);
});

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

static IAsyncPolicy<HttpResponseMessage> GetBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

var customerBaseUrl = builder.Configuration["Services:Customer"] ?? "http://customer-api:8080";
var productBaseUrl  = builder.Configuration["Services:Product"]  ?? "http://product-api:8080";

builder.Services
    .AddRefitClient<ICustomerClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(customerBaseUrl))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetBreakerPolicy());

builder.Services
    .AddRefitClient<IProductClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(productBaseUrl))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetBreakerPolicy());

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var o = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    return new ConnectionFactory
    {
        HostName = o.Host,
        Port = o.Port,
        UserName = o.User,
        Password = o.Pass,
        VirtualHost = o.VirtualHost,
        DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
    };
});

builder.Services.AddSingleton<IConnection>(sp =>
    sp.GetRequiredService<IConnectionFactory>().CreateConnection("order-api"));

builder.Services.AddSingleton<IModel>(sp =>
    sp.GetRequiredService<IConnection>().CreateModel());

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OrderEventsAuditConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1");
    c.RoutePrefix = "swagger";
});

app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.Run();
