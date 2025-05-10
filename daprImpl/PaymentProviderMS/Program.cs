
using OnlineMarket.Core.PaymentProviderLibrary.Infra;
using OnlineMarket.Core.PaymentProviderLibrary.Service;
using OnlineMarket.Core.PaymentProviderLibrary.Services;

var builder = WebApplication.CreateBuilder(args);

// 加载配置
IConfigurationSection configSection = builder.Configuration.GetSection("PaymentProviderConfig");
var config = configSection.Get<PaymentProviderConfig>();
// if (config == null)
//     Environment.Exit(1);

// 注入 Core 层的 PaymentProviderServiceCore（用工厂方式注入 config）
builder.Services.AddSingleton<IPaymentProvider>(provider =>
{
    return new PaymentProviderServiceCore(config);
});

// Controller + Swagger + 健康检查
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();