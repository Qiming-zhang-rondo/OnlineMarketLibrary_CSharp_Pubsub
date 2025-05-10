using System;
using OnlineMarket.Core.CustomerLibrary.Models;

namespace OnlineMarket.Core.CustomerLibrary.Repositories;

public interface ICustomerRepository
{
    CustomerModel? GetById(int customerId);
    CustomerModel Insert(CustomerModel customer);
    CustomerModel Update(CustomerModel customer);

    void Cleanup();
    void Reset();

}

