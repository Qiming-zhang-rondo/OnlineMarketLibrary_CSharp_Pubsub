using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.PaymentLibrary.Services
{
    /**
     * 
     * Based on
     * https://stripe.com/docs/payments/quickstart?lang=dotnet&client=java&platform=android
     * 
     * https://stripe.com/docs/api
     * "The Stripe API is organized around REST."
     * 
     * https://stripe.com/docs/payments/payment-methods#webhooks
     * "Use webhooks for payment methods that either require customer action or when payment notification is delayed."
     * Thus, we assume no webhooks in the benchmark.
     * 
     */
    public interface IExternalProvider
	{
        PaymentIntent? Create(PaymentIntentCreateOptions options);
    }
}

