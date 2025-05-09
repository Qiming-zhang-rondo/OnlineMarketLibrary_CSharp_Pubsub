using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.CartLibrary.Services;
using OnlineMarket.Core.CartLibrary.Infra;
using OnlineMarket.Common.Messaging;
using Dapr.Client;
using CartMS.Repositories.Impl;
using OnlineMarket.DaprImpl.CartMS.Gateways;

var builder = WebApplication.CreateBuilder(args);

// 加载 Cart 配置
builder.Services.AddOptions();
builder.Services.Configure<CartConfig>(builder.Configuration.GetSection("CartConfig"));

// InMemory 实现
builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
builder.Services.AddSingleton<IProductReplicaRepository, InMemoryProductReplicaRepository>();

// EventPublisher 实现（Dapr）
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// Core Service
builder.Services.AddScoped<ICartService, CartServiceCore>();

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