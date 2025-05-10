using System.Text;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.StockLibrary.Infra;
using OnlineMarket.Core.StockLibrary.Models;
using OnlineMarket.Core.StockLibrary.Services;
using StockMS.Repositories;
using StockMS.Services;

namespace OnlineMarket.Core.StockLibrary.Services;

public class StockServiceCore : IStockService
{
    private readonly IStockRepository stockRepository;
    private readonly IEventPublisher eventPublisher;
    private readonly StockConfig config;
    private readonly ILogger<StockServiceCore> logger;

    public StockServiceCore(
        IStockRepository stockRepository,
        IEventPublisher eventPublisher,
        StockConfig config,
        ILogger<StockServiceCore> logger)
    {
        this.stockRepository = stockRepository;
        this.eventPublisher = eventPublisher;
        this.config = config;
        this.logger = logger;
    }

    /* ---------- 静态流 ID ---------- */

    private static readonly string ProductUpdateStreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.UPDATE_PRODUCT.ToString())
            .ToString();

    private static readonly string ReserveStreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.CUSTOMER_SESSION.ToString())
            .ToString();

    /* ---------- 产品信息更新 ---------- */

    public async Task ProcessProductUpdate(ProductUpdated productUpdated)
    {
        using (var txCtx = this.stockRepository.BeginTransaction())
        {
            var stockItem =
                this.stockRepository.FindForUpdate(productUpdated.seller_id, productUpdated.product_id)
                ?? throw new ApplicationException(
                    $"Stock item not found {productUpdated.seller_id}-{productUpdated.product_id}");

            stockItem.version = productUpdated.version;
            this.stockRepository.Update(stockItem);
            txCtx.Commit();
        }

        if (this.config.Streaming)
        {
            var mark = new TransactionMark(
                productUpdated.version,
                TransactionType.UPDATE_PRODUCT,
                productUpdated.seller_id,
                MarkStatus.SUCCESS,
                "stock");

            await this.eventPublisher.PublishEventAsync(ProductUpdateStreamId, mark);
        }
    }

    public async Task ProcessPoisonProductUpdate(ProductUpdated productUpdate)
    {
        var mark = new TransactionMark(
            productUpdate.version,
            TransactionType.UPDATE_PRODUCT,
            productUpdate.seller_id,
            MarkStatus.ABORT,
            "stock");

        await this.eventPublisher.PublishEventAsync(ProductUpdateStreamId, mark);
    }

    /* ---------- 预留库存 ---------- */

    public async Task ReserveStockAsync(ReserveStock checkout)
    {
        var ids = checkout.items.Select(c => (c.SellerId, c.ProductId)).ToList();

        List<CartItem> reserved = new();
        List<ProductStatus> unavailable = new();
        List<StockItemModel> stockUpdates = new();
        var now = DateTime.UtcNow;

        using (var txCtx = this.stockRepository.BeginTransaction())
        {
            var items = this.stockRepository.GetItems(ids).ToDictionary(i => (i.seller_id, i.product_id));

            foreach (var item in checkout.items)
            {
                if (!items.TryGetValue((item.SellerId, item.ProductId), out var stockItem) ||
                    stockItem.version != item.Version)
                {
                    unavailable.Add(new ProductStatus(item.ProductId, ItemStatus.UNAVAILABLE));
                    continue;
                }

                if (stockItem.qty_available < stockItem.qty_reserved + item.Quantity)
                {
                    unavailable.Add(new ProductStatus(
                        item.ProductId,
                        ItemStatus.OUT_OF_STOCK,
                        stockItem.qty_available));
                    continue;
                }

                stockItem.qty_reserved += item.Quantity;
                stockItem.updated_at = now;
                reserved.Add(item);
                stockUpdates.Add(stockItem);
            }

            if (stockUpdates.Count > 0)
            {
                this.stockRepository.UpdateRange(stockUpdates);
                this.stockRepository.FlushUpdates();
                txCtx.Commit();
            }
        }

        /* ---------- 事件发布 ---------- */

        if (!this.config.Streaming) return;

        // 已成功预留的商品
        if (reserved.Count > 0)
        {
            var confirmed = new StockConfirmed(
                checkout.timestamp,
                checkout.customerCheckout,
                reserved,
                checkout.instanceId);

            await this.eventPublisher.PublishEventAsync(nameof(StockConfirmed), confirmed);
        }

        // 预留失败的商品
        if (unavailable.Count > 0 && this.config.RaiseStockFailed)
        {
            var failed = new ReserveStockFailed(
                checkout.timestamp,
                checkout.customerCheckout,
                unavailable,
                checkout.instanceId);

            await this.eventPublisher.PublishEventAsync(nameof(ReserveStockFailed), failed);
        }

        // 整单未被接受
        if (reserved.Count == 0)
        {
            var mark = new TransactionMark(
                checkout.instanceId,
                TransactionType.CUSTOMER_SESSION,
                checkout.customerCheckout.CustomerId,
                MarkStatus.NOT_ACCEPTED,
                "stock");

            await this.eventPublisher.PublishEventAsync(ReserveStreamId, mark);
        }
    }

    public async Task ProcessPoisonReserveStock(ReserveStock reserveStock)
    {
        var mark = new TransactionMark(
            reserveStock.instanceId,
            TransactionType.CUSTOMER_SESSION,
            reserveStock.customerCheckout.CustomerId,
            MarkStatus.ABORT,
            "stock");

        await this.eventPublisher.PublishEventAsync(ReserveStreamId, mark);
    }

    /* ---------- 付款结果处理 ---------- */

    public void CancelReservation(PaymentFailed payment)
    {
        UpdateReservedQuantities(
            payment.items,
            (stock, qty) => stock.qty_reserved -= qty);
    }

    public void ConfirmReservation(PaymentConfirmed payment)
    {
        UpdateReservedQuantities(
            payment.items,
            (stock, qty) =>
            {
                stock.qty_available -= qty;
                stock.qty_reserved -= qty;
                stock.order_count++;
            });
    }

    private void UpdateReservedQuantities(IEnumerable<OrderItem> items, Action<StockItemModel, int> update)
    {
        var now = DateTime.UtcNow;
        var ids = items.Select(p => (p.seller_id, p.product_id)).ToList();

        using (var txCtx = this.stockRepository.BeginTransaction())
        {
            var stocks = this.stockRepository.GetItems(ids)
                           .ToDictionary(p => (p.seller_id, p.product_id));

            foreach (var item in items)
            {
                var stock = stocks[(item.seller_id, item.product_id)];
                update(stock, item.quantity);
                stock.updated_at = now;
                this.stockRepository.Update(stock);
            }

            this.stockRepository.FlushUpdates();
            txCtx.Commit();
        }
    }

    /* ---------- 增加库存 ---------- */

    public async Task IncreaseStock(IncreaseStock increaseStock)
    {
        using (var txCtx = this.stockRepository.BeginTransaction())
        {
            var item = this.stockRepository.Find(increaseStock.seller_id, increaseStock.product_id)
                       ?? throw new Exception($"Attempt to lock item {increaseStock.product_id} failed");

            item.qty_available += increaseStock.quantity;
            this.stockRepository.Update(item);
            txCtx.Commit();
            if (this.config.Streaming)
            {
                await this.eventPublisher.PublishEventAsync(nameof(StockItem), new StockItem()
                {
                    seller_id = item.seller_id,
                    product_id = item.product_id,
                    qty_available = item.qty_available,
                    qty_reserved = item.qty_reserved,
                    order_count = item.order_count,
                    ytd = item.ytd,
                    data = item.data
                });
            }
        }

        // if (this.config.Streaming)
        // {
        //     await this.eventPublisher.PublishEventAsync(nameof(StockItem), new StockItem()
        //     {
        //         seller_id = increaseStock.seller_id,
        //         product_id = increaseStock.product_id,
        //         qty_available = increaseStock.quantity,
        //         qty_reserved = 0,
        //         order_count = 0,
        //         ytd = 0,
        //         data = string.Empty,
        //         version = increaseStock.version
        //     });
    }

    /* ---------- 创建或更新库存记录 ---------- */

    public Task CreateStockItem(StockItem stockItem)
    {
        var model = new StockItemModel
        {
            product_id = stockItem.product_id,
            seller_id = stockItem.seller_id,
            qty_available = stockItem.qty_available,
            qty_reserved = stockItem.qty_reserved,
            order_count = stockItem.order_count,
            ytd = stockItem.ytd,
            data = stockItem.data,
            version = stockItem.version
        };

        using (var txCtx = this.stockRepository.BeginTransaction())
        {
            if (this.stockRepository.Find(model.seller_id, model.product_id) is null)
                this.stockRepository.Insert(model);
            else
                this.stockRepository.Update(model);

            this.stockRepository.FlushUpdates();
            txCtx.Commit();
        }
        return Task.CompletedTask;
    }

    /* ---------- 环境维护 ---------- */

    public void Cleanup() => this.stockRepository.Cleanup();

    public void Reset() => this.stockRepository.Reset(this.config.DefaultInventory);
}
