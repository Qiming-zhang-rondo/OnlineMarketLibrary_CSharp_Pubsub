// tests/StockMS.DaprTests/Fakes/DaprEventPublisher.cs
using Dapr.Client;
using OnlineMarket.Common.Messaging;

public sealed class DaprEventPublisher : IEventPublisher
{
    private readonly DaprClient dapr;
    public DaprEventPublisher(DaprClient dapr) => this.dapr = dapr;

    public Task PublishEventAsync(string topic, object payload)
        => dapr.PublishEventAsync("pubsub", topic, payload);
}