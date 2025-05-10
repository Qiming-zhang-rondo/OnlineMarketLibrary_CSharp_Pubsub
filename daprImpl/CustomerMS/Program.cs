using OnlineMarket.Core.CustomerLibrary.Repositories;
using OnlineMarket.Core.CustomerLibrary.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.DaprImpl.CustomerMS.Repositories;

var builder = WebApplication.CreateBuilder(args);

// InMemory 实现
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerServiceCore>();

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