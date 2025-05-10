using OnlineMarket.Core.SellerLibrary.Repositories;
using OnlineMarket.Core.SellerLibrary.Services;
using OnlineMarket.Core.StockLibrary.Services;
using SellerMS.Repositories;


var builder = WebApplication.CreateBuilder(args);

// InMemory 实现
builder.Services.AddSingleton<ISellerRepository, InMemorySellerRepository>();
builder.Services.AddScoped<ISellerService, SellerServiceCore>();

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