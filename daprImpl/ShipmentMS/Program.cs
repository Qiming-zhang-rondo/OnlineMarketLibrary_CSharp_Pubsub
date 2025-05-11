using Microsoft.AspNetCore.Mvc;
using Dapr.Client;

using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.ShipmentLibrary.Infra;
using OnlineMarket.Core.ShipmentLibrary.Repositories;
using OnlineMarket.DaprImpl.ShipmentMS.Repositories;
using OnlineMarket.DaprImpl.ShipmentMS.Gateways;
using OnlineMarket.Core.ShipmentLibrary.Services;


var builder = WebApplication.CreateBuilder(args);

// 加载配置
builder.Services.AddOptions();
builder.Services.Configure<ShipmentConfig>(builder.Configuration.GetSection("ShipmentConfig"));
var config = builder.Configuration.GetSection("ShipmentConfig").Get<ShipmentConfig>();
if (config == null)
    Environment.Exit(1);

// 使用 InMemory 实现
builder.Services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
builder.Services.AddSingleton<IPackageRepository, InMemoryPackageRepository>();

// Dapr 事件发布
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// 注入 ShipmentServiceCore（含 logger 和 config）
builder.Services.AddScoped<IShipmentService>(provider =>
{
    var shipmentRepo = provider.GetRequiredService<IShipmentRepository>();
    var packageRepo = provider.GetRequiredService<IPackageRepository>();
    var eventPublisher = provider.GetRequiredService<IEventPublisher>();
    var logger = provider.GetRequiredService<ILogger<ShipmentServiceCore>>();

    return new ShipmentServiceCore(shipmentRepo, packageRepo, eventPublisher, config, logger);
});

builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr();

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (config.Streaming)
    app.UseCloudEvents();

app.MapControllers();
app.MapHealthChecks("/health");

if (config.Streaming)
    app.MapSubscribeHandler();

app.Run();