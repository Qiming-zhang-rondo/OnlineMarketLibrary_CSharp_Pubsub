using OnlineMarket.Core.SellerLibrary.Infra;
using OnlineMarket.Core.SellerLibrary.Repositories;
using OnlineMarket.Core.SellerLibrary.Services;
using OnlineMarket.DaprImpl.SellerMS.Repositories;


var builder = WebApplication.CreateBuilder(args);

// 加载配置
builder.Services.AddOptions();
builder.Services.Configure<SellerConfig>(builder.Configuration.GetSection("SellerConfig"));
var config = builder.Configuration.GetSection("SellerConfig").Get<SellerConfig>();
if (config == null)
    Environment.Exit(1);

// InMemory 实现
builder.Services.AddSingleton<ISellerRepository, InMemorySellerRepository>();

// 注入核心服务（带 config）
builder.Services.AddScoped<ISellerService>(provider =>
{
    var repo = provider.GetRequiredService<ISellerRepository>();
    var logger = provider.GetRequiredService<ILogger<SellerServiceCore>>();
    return new SellerServiceCore(repo, config, logger);
});

// Dapr、控制器、健康检查等
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