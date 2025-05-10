using Microsoft.Extensions.Options;
using OnlineMarket.Core.PaymentLibrary.Infra;
using OnlineMarket.Core.Common.Integration;
using Newtonsoft.Json;
using System.Text;
using OnlineMarket.Core.PaymentLibrary.Services;

namespace OnlineMarket.DaprImpl.PaymentMS.Services
{
	public class ExternalProviderProxy : IExternalProvider
	{
        private readonly HttpClient httpClient;
        private readonly PaymentConfig config;

		public ExternalProviderProxy(HttpClient httpClient, IOptions<PaymentConfig> config, ILogger<ExternalProviderProxy> logger)
		{
            this.httpClient = httpClient;
            this.config = config.Value;
		}

        public PaymentIntent? Create(PaymentIntentCreateOptions options)
        {
            // perform http request
            var msg = new HttpRequestMessage(HttpMethod.Post, config.PaymentProviderUrl);
            var payload = JsonConvert.SerializeObject(options);
            msg.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.Send(msg);

            var stream = response.Content.ReadAsStream();
            StreamReader reader = new StreamReader(stream);
            string intentRet = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<PaymentIntent>(intentRet);

        }
    }
}
