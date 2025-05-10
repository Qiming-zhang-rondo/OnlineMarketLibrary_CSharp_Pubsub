using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.StockLibrary.Infra;
using OnlineMarket.Core.StockLibrary.Models;

namespace StockMS.Repositories;

public sealed class InMemoryStockRepository : IStockRepository
{
    private readonly IDictionary<(int sellerId, int productId), StockItemModel> stockItems;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryStockRepository(IOptions<StockConfig> config)
	{
        this.stockItems = new ConcurrentDictionary<(int sellerId, int productId), StockItemModel>();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "stock");
	}

    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
    }

    public StockItemModel? Find(int sellerId, int productId)
    {
        if (this.stockItems.ContainsKey((sellerId, productId)))
        {
            return this.stockItems[(sellerId, productId)];
        }
        return null;
    }

    public StockItemModel FindForUpdate(int sellerId, int productId)
    {
        var item = this.Find(sellerId, productId);
        return item is null ? throw new ApplicationException($"Cannot find stock item {sellerId}-{productId}") : item;
    }

    public void FlushUpdates()
    {
        // do nothing
    }

    public IEnumerable<StockItemModel> GetAll()
    {
        return this.stockItems.Values;
    }

    public IEnumerable<StockItemModel> GetBySellerId(int sellerId)
    {
        return this.stockItems.Values.Where(i=>i.seller_id == sellerId);
    }

    public IEnumerable<StockItemModel> GetItems(List<(int SellerId, int ProductId)> ids)
    {
        List<StockItemModel> list = new List<StockItemModel>();
        foreach(var (SellerId, ProductId) in ids)
        {
            list.Add( this.stockItems[(SellerId,ProductId)] );
        }
        return list;
    }

    public StockItemModel Insert(StockItemModel item)
    {
        item.active = true;
        item.created_at = DateTime.Now;
        item.updated_at = item.created_at;
        this.stockItems.TryAdd((item.seller_id, item.product_id), item);
        this.logging.Append(item);
        return item;
    }

    public void Update(StockItemModel item)
    {
        item.active = true;
        item.created_at = this.stockItems[(item.seller_id, item.product_id)].created_at;
        item.updated_at = DateTime.UtcNow;
        this.stockItems[(item.seller_id, item.product_id)] = item;
        this.logging.Append(item);
    }

    public void UpdateRange(List<StockItemModel> stockItemsReserved)
    {
        foreach(var item in stockItemsReserved)
        {
            this.Update(item);
        }
    }

    public void Cleanup()
    {
        this.stockItems.Clear();
        this.logging.Clear();
    }

    public void Reset(int qty)
    {
        foreach(var item in this.stockItems.Values)
        {
            item.qty_available = qty;
            item.version = "0";
            item.qty_reserved = 0;
        }
        this.logging.Clear();
    }

    public class NoTransactionScope : IDbContextTransaction
    {
        public Guid TransactionId => throw new NotImplementedException();

        public void Commit()
        {
            // do nothing
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // do nothing
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

}