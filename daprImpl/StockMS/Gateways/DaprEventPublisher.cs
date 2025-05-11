using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Common.Messaging;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OnlineMarket.DaprImpl.StockMS.Gateways
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
        // 先序列化 payload
        // var payload = JsonConvert.SerializeObject(eventObj, Formatting.Indented);
        // 用 Console.WriteLine 打出来
        // Console.WriteLine($"[PublishEvent] → topic = {topic}\n{payload}\n");

        try
        {
            await daprClient.PublishEventAsync(PUBSUB_NAME, topic, eventObj);
            // Console.WriteLine($"[PublishEvent] ✔ published to {topic}");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"[PublishEvent] ✖ failed to publish to {topic}: {ex.Message}");
        }
    }
    }
}