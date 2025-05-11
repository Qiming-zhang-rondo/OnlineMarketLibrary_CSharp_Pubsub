using OnlineMarket.Core.StockLibrary.Services;
using OnlineMarket.Core.StockLibrary.Infra;
using OnlineMarket.Common.Messaging;
using OnlineMarket.DaprImpl.StockMS.Repositories;
using OnlineMarket.DaprImpl.StockMS.Gateways;
using OnlineMarket.Core.StockLibrary.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 加载配置
builder.Services.AddOptions();
builder.Services.Configure<StockConfig>(builder.Configuration.GetSection("StockConfig"));
var config = builder.Configuration.GetSection("StockConfig").Get<StockConfig>();
if (config == null)
    Environment.Exit(1);

// 注册仓储 & publisher
builder.Services.AddSingleton<IStockRepository, InMemoryStockRepository>();
builder.Services.AddScoped<IEventPublisher, DaprEventPublisher>();

// 注入 core service
builder.Services.AddScoped<IStockService>(provider => {
    var repo = provider.GetRequiredService<IStockRepository>();
    var publisher = provider.GetRequiredService<IEventPublisher>();
    var logger = provider.GetRequiredService<ILogger<StockServiceCore>>();
    return new StockServiceCore(repo, publisher, config, logger);
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

app.UseCloudEvents();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapSubscribeHandler();

app.Run();