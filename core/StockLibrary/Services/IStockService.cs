using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.StockLibrary.Services
{
    public interface IStockService
    {
        Task ReserveStockAsync(ReserveStock checkout);

        void ConfirmReservation(PaymentConfirmed payment);

        void CancelReservation(PaymentFailed paymentFailure);

        Task ProcessProductUpdate(ProductUpdated productUpdate);

        Task CreateStockItem(StockItem stockItem);

        Task IncreaseStock(IncreaseStock increaseStock);

        void Cleanup();
        void Reset();

        Task ProcessPoisonReserveStock(ReserveStock reserveStock);
        Task ProcessPoisonProductUpdate(ProductUpdated productUpdate);

    }
}