namespace OnlineMarket.Common.Messaging;

using System.Threading.Tasks;

public interface IEventPublisher
{
    Task PublishEventAsync(string topic, object @event);
}