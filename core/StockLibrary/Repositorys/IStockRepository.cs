using Microsoft.EntityFrameworkCore.Storage;
using OnlineMarket.Core.StockLibrary.Models;

namespace OnlineMarket.Core.StockLibrary.Repositories;

public interface IStockRepository
{
    StockItemModel Insert(StockItemModel item);

    void Update(StockItemModel item);

    StockItemModel? Find(int sellerId, int productId);

    IEnumerable<StockItemModel> GetAll();

    IEnumerable<StockItemModel> GetItems(List<(int SellerId, int ProductId)> ids);
    IEnumerable<StockItemModel> GetBySellerId(int sellerId);

    StockItemModel FindForUpdate(int seller_id, int product_id);

    // APIs for StockService
    IDbContextTransaction BeginTransaction();
    void FlushUpdates();
    void UpdateRange(List<StockItemModel> stockItemsReserved);
    void Reset(int qty);
    void Cleanup();
}