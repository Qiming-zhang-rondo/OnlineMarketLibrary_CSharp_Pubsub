using OnlineMarket.Core.StockLibrary.Services;
using StockMS.Repositories;
using StockMS.Services;

var builder = WebApplication.CreateBuilder(args);

// InMemory 实现
builder.Services.AddSingleton<IStockRepository, InMemoryStockRepository>();
builder.Services.AddScoped<IStockService, StockServiceCore>();

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