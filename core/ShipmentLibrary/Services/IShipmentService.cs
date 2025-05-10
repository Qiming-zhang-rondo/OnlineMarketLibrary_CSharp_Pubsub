
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.ShipmentLibrary.Services;

public interface IShipmentService
{
    public Task ProcessShipment(PaymentConfirmed paymentResult);

    public Task UpdateShipment(string instanceId);

    void Cleanup();

    Task ProcessPoisonShipment(PaymentConfirmed paymentRequest);
}