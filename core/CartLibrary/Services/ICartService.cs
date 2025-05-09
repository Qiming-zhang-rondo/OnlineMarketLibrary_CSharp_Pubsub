using OnlineMarket.Core.CartLibrary.Models;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.CartLibrary.Services;

public interface ICartService
{
    // can also be used for test
    void Seal(CartModel cart, bool cleanItems = true);

    Task NotifyCheckout(CustomerCheckout customerCheckout);

    void Cleanup();

    void ProcessProductUpdated(ProductUpdated productUpdated);
    Task ProcessPriceUpdate(PriceUpdated updatePrice);

    void Reset();

    Task ProcessPoisonProductUpdated(ProductUpdated productUpdated);
    Task ProcessPoisonPriceUpdate(PriceUpdated update);
    Task ProcessPoisonCheckout(CustomerCheckout customerCheckout, MarkStatus status);
}

