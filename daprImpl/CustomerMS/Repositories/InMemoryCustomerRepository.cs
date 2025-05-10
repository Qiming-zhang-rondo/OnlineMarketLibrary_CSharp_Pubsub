using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.CustomerLibrary.Infra;
using OnlineMarket.Core.CustomerLibrary.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using OnlineMarket.Core.CustomerLibrary.Repositories;

namespace OnlineMarket.DaprImpl.CustomerMS.Repositories;

public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<int, CustomerModel> customers;

    private readonly ILogging logging;

    public InMemoryCustomerRepository(IOptions<CustomerConfig> config)
    {
        this.customers = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "customer");
    }

    public CustomerModel Insert(CustomerModel customer)
    {
        customer.created_at = DateTime.UtcNow;
        customer.updated_at = customer.created_at;
        this.customers.TryAdd(customer.id, customer);
        this.logging.Append(customer);
        return customer;
    }

    public CustomerModel Update(CustomerModel customer)
    {
        customer.updated_at = DateTime.UtcNow;
        this.customers[customer.id] = customer;
        this.logging.Append(customer);
        return customer;
    }

    public CustomerModel? GetById(int id)
    {
        return this.customers[id];
    }

    public void Cleanup()
    {
        this.customers.Clear();
        this.logging.Clear();
    }

    public void Reset()
    {
        foreach(var cust in this.customers.Values){
            cust.delivery_count = 0;
            cust.failed_payment_count = 0;
            cust.success_payment_count = 0;
        }
        this.logging.Clear();
    }

}

