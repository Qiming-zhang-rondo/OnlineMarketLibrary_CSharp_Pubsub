using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.ProductLibrary.Repositories;
using OnlineMarket.Core.ProductLibrary.Services;
using OnlineMarket.Core.ProductLibrary.Infra;
using OnlineMarket.Common.Messaging;
using Dapr.Client;

using ProductMS.Repositories;
using OnlineMarket.DaprImpl.ProductMS.Gateways;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 加载 Product 配置
builder.Services.AddOptions();
builder.Services.Configure<ProductConfig>(builder.Configuration.GetSection("ProductConfig"));
var config = builder.Configuration.GetSection("ProductConfig").Get<ProductConfig>();
if (config == null)
    Environment.Exit(1);

// InMemory 仓储实现
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// EventPublisher 实现（Dapr）
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// 注入 ProductServiceCore（用工厂注入 config）
builder.Services.AddScoped<IProductService>(provider =>
{
    var repo = provider.GetRequiredService<IProductRepository>();
    var eventPublisher = provider.GetRequiredService<IEventPublisher>();
    return new ProductServiceCore(repo, eventPublisher, config);
});

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

if (config.Streaming)
    app.UseCloudEvents();

app.MapControllers();
app.MapHealthChecks("/health");

if (config.Streaming)
    app.MapSubscribeHandler(); // 订阅用的

app.Run();