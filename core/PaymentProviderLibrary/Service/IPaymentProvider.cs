using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.PaymentProviderLibrary.Service
{
    public interface IPaymentProvider
    {
        PaymentIntent ProcessPaymentIntent(PaymentIntentCreateOptions options);
    }
}

