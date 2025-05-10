using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Common.Messaging;
using System.Threading.Tasks;

namespace OnlineMarket.DaprImpl.OrderMS.Gateways
{
    public class DaprEventPublisher : IEventPublisher
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<DaprEventPublisher> logger;
        private const string PUBSUB_NAME = "pubsub";

        public DaprEventPublisher(DaprClient daprClient, ILogger<DaprEventPublisher> logger)
        {
            this.daprClient = daprClient;
            this.logger = logger;
        }

        public async Task PublishEventAsync(string topic, object eventObj)
        {
            try
            {
                await daprClient.PublishEventAsync(PUBSUB_NAME, topic, eventObj);
                logger.LogInformation("Published event to topic {Topic}", topic);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish event to topic {Topic}", topic);
            }
        }
    }
}