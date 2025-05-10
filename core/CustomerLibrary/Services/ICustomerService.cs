

using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.CustomerLibrary.Services
{
    public interface ICustomerService
    {
        void ProcessDeliveryNotification(DeliveryNotification paymentConfirmed);
        void ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed);
        void ProcessPaymentFailed(PaymentFailed paymentFailed);

        void Cleanup();
        void Reset();
    }
}

