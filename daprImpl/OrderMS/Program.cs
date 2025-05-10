using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.OrderLibrary.Repositories;
using OnlineMarket.Core.OrderLibrary.Services;
using OnlineMarket.Core.OrderLibrary.Infra;
using OnlineMarket.Common.Messaging;
using Dapr.Client;

using OnlineMarket.DaprImpl.OrderMS.Gateways;
using OnlineMarket.Core.OrderMS.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 加载 Order 配置
builder.Services.AddOptions();
builder.Services.Configure<OrderConfig>(builder.Configuration.GetSection("OrderConfig"));

// InMemory 实现
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// EventPublisher 实现（Dapr）
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// Core Service
builder.Services.AddScoped<IOrderService, OrderServiceCore>();

// Dapr client & Controllers
builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr(); // 让 [Topic] 生效

// Swagger + 健康检查（可选）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCloudEvents();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapSubscribeHandler(); // 订阅用的

app.Run();