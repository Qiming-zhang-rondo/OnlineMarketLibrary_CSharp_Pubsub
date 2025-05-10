using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.PaymentLibrary.Services
{
	public interface IPaymentService
	{
        Task ProcessPayment(InvoiceIssued paymentRequest);
        void Cleanup();
        Task ProcessPoisonPayment(InvoiceIssued invoice);
    }
}

