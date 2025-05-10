using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.CustomerLibrary.Repositories;
using OnlineMarket.Core.CustomerLibrary.Models;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.CustomerLibrary.Services;

namespace OnlineMarket.Core.CustomerLibrary.Services;

public class CustomerServiceCore : ICustomerService
{
    private readonly ICustomerRepository customerRepository;
    private readonly ILogger<CustomerServiceCore> logger;

    public CustomerServiceCore(ICustomerRepository customerRepository, ILogger<CustomerServiceCore> logger)
    {
        this.customerRepository = customerRepository;
        this.logger = logger;
    }

    public void ProcessDeliveryNotification(DeliveryNotification deliveryNotification)
    {
        var customer = this.customerRepository.GetById(deliveryNotification.customerId);
        if (customer is not null)
        {
            customer.delivery_count++;
            this.customerRepository.Update(customer);
        }
    }

    public void ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        var customer = this.customerRepository.GetById(paymentConfirmed.customer.CustomerId);
        if (customer is not null)
        {
            customer.success_payment_count++;
            this.customerRepository.Update(customer);
        }
    }

    public void ProcessPaymentFailed(PaymentFailed paymentFailed)
    {
        var customer = this.customerRepository.GetById(paymentFailed.customer.CustomerId);
        if (customer is not null)
        {
            customer.failed_payment_count++;
            this.customerRepository.Update(customer);
        }
    }

    public void Cleanup()
    {
        this.customerRepository.Cleanup();
    }

    public void Reset()
    {
        this.customerRepository.Reset();
    }
}