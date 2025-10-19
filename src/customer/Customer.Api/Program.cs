using Customer.Api.Startup;
using Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var conn =
    builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["ConnectionStrings:Postgres"];

builder.Services.AddDbContext<CustomerDbContext>(opt =>
    opt.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "customer")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer API v1");
    c.RoutePrefix = "swagger";
});


app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("CustomerBootstrap");
    db.Database.Migrate();
    await CustomerSeed.EnsureAsync(db, logger);
}

app.Run();