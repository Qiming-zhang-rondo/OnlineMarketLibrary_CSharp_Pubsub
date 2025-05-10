using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.PaymentLibrary.Repositories;
using OnlineMarket.Core.PaymentLibrary.Services;
using OnlineMarket.Core.PaymentLibrary.Infra;
using OnlineMarket.Common.Messaging;
using Dapr.Client;

using OnlineMarket.DaprImpl.PaymentMS.Gateways;
using OnlineMarket.DaprImpl.PaymentMS.Repositories;
using OnlineMarket.DaprImpl.PaymentMS.Services;

var builder = WebApplication.CreateBuilder(args);

// 加载 Payment 配置
builder.Services.AddOptions();
builder.Services.Configure<PaymentConfig>(builder.Configuration.GetSection("PaymentConfig"));
var config = builder.Configuration.GetSection("PaymentConfig").Get<PaymentConfig>();
if (config == null)
    Environment.Exit(1);

// InMemory 实现
builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

// HttpClient（注入给 ExternalProviderProxy）
builder.Services.AddSingleton<HttpClient>();

// 注入 IExternalProvider 实现
builder.Services.AddScoped<IExternalProvider, ExternalProviderProxy>();

// 注入 IEventPublisher 实现（Dapr）
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// 注入 PaymentServiceCore（用工厂方式传入 config）
builder.Services.AddScoped<IPaymentService>(provider =>
{
    var repo = provider.GetRequiredService<IPaymentRepository>();
    var externalProvider = provider.GetRequiredService<IExternalProvider>();
    var eventPublisher = provider.GetRequiredService<IEventPublisher>();
    return new PaymentServiceCore(repo, externalProvider, eventPublisher, config);
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

app.UseCloudEvents();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapSubscribeHandler(); // 订阅用的

app.Run();