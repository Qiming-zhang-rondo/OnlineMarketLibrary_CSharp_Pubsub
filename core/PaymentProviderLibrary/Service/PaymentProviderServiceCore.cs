using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Integration;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.PaymentProviderLibrary.Infra;
using OnlineMarket.Core.PaymentProviderLibrary.Service;
using Microsoft.Extensions.Logging;

namespace OnlineMarket.Core.PaymentProviderLibrary.Services
{
	public class PaymentProviderServiceCore : IPaymentProvider
	{
        private readonly PaymentProviderConfig config;

        private readonly IDictionary<string, PaymentIntent> db;

        public PaymentProviderServiceCore(PaymentProviderConfig config)
        {
            this.config = config;
            this.db = new ConcurrentDictionary<string, PaymentIntent>();
        }

        public PaymentIntent ProcessPaymentIntent(PaymentIntentCreateOptions options)
        {
            // check first
            if (db.ContainsKey(options.IdempotencyKey))
            {
                return db[options.IdempotencyKey];
            }
            var random = new Random();
            var failProb = random.Next(1, config.FailPercentage + 1);
            string status = "succeeded";
            if (failProb <= config.FailPercentage)
            {
                status = "canceled";   
            }
            var intent = new PaymentIntent()
            {
                id = Guid.NewGuid().ToString(),
                amount = options.Amount,
                client_secret = "",
                currency = options.Currency.ToString(),
                customer = options.Customer,
                status = status,
                created = DateTime.UtcNow.Millisecond
            };
            if (db.TryAdd(options.IdempotencyKey, intent))
            {
                return intent;
            }
            return db[options.IdempotencyKey];
        }
    }
}

