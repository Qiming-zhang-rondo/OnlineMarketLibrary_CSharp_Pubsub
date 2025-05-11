using System.Text;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.StockLibrary.Infra;
using OnlineMarket.Core.StockLibrary.Models;
using OnlineMarket.Core.StockLibrary.Services;
using OnlineMarket.Core.StockLibrary.Repositories;

namespace OnlineMarket.Core.StockLibrary.Services
{
    public class StockServiceCore : IStockService
    {
        private readonly IStockRepository stockRepository;
        private readonly IEventPublisher eventPublisher;
        private readonly StockConfig config;
        private readonly ILogger<StockServiceCore> logger;

        public StockServiceCore(IStockRepository stockRepository, IEventPublisher eventPublisher, StockConfig config, ILogger<StockServiceCore> logger)
        {
            this.stockRepository = stockRepository;
            this.eventPublisher = eventPublisher;
            this.config = config;
            this.logger = logger;
        }

        static readonly string streamUpdateId = $"TransactionMark_{TransactionType.UPDATE_PRODUCT}";
        static readonly string streamReserveId = $"TransactionMark_{TransactionType.CUSTOMER_SESSION}";

        public async Task ProcessProductUpdate(ProductUpdated productUpdated)
        {
            using (var tx = stockRepository.BeginTransaction())
            {
                var stockItem = stockRepository.FindForUpdate(productUpdated.seller_id, productUpdated.product_id);
                if (stockItem == null)
                    throw new ApplicationException($"Stock item not found {productUpdated.seller_id}-{productUpdated.product_id}");

                stockItem.version = productUpdated.version;
                stockRepository.Update(stockItem);
                tx.Commit();
            }

            if (config.Streaming)
            {
                await eventPublisher.PublishEventAsync(streamUpdateId, new TransactionMark(productUpdated.version, TransactionType.UPDATE_PRODUCT, productUpdated.seller_id, MarkStatus.SUCCESS, "stock"));
            }
        }

        public async Task ProcessPoisonProductUpdate(ProductUpdated productUpdate)
        {
            await eventPublisher.PublishEventAsync(streamUpdateId, new TransactionMark(productUpdate.version, TransactionType.UPDATE_PRODUCT, productUpdate.seller_id, MarkStatus.ABORT, "stock"));
        }

        public async Task ReserveStockAsync(ReserveStock checkout)
        {
            var ids = checkout.items.Select(c => (c.SellerId, c.ProductId)).ToList();
            using (var txCtx = stockRepository.BeginTransaction())
            {
                IEnumerable<StockItemModel> items = stockRepository.GetItems(ids);
                if (!items.Any())
                {
                    logger.LogCritical("No items in checkout were retrieved from Stock state: \n{Checkout}", checkout);
                    await eventPublisher.PublishEventAsync(streamUpdateId, new TransactionMark(checkout.instanceId, TransactionType.CUSTOMER_SESSION, checkout.customerCheckout.CustomerId, MarkStatus.ERROR, "stock"));
                    return;
                }

                var stockItems = items.ToDictionary(i => (i.seller_id, i.product_id));
                List<ProductStatus> unavailableItems = new();
                List<CartItem> cartItemsReserved = new();
                List<StockItemModel> stockItemsReserved = new();
                var now = DateTime.UtcNow;

                foreach (var item in checkout.items)
                {
                    if (!stockItems.ContainsKey((item.SellerId, item.ProductId)) || stockItems[(item.SellerId, item.ProductId)].version != item.Version)
                    {
                        // Console.WriteLine($"[Check] No stock found for seller={item.SellerId}, product={item.ProductId}");
                        unavailableItems.Add(new ProductStatus(item.ProductId, ItemStatus.UNAVAILABLE));
                        continue;
                    }

                    var stockItem = stockItems[(item.SellerId, item.ProductId)];
                    // Console.WriteLine($"[Check] seller={item.SellerId}, product={item.ProductId}, stock version={stockItem.version}, checkout version={item.Version}");

                    if (stockItem.qty_available < (stockItem.qty_reserved + item.Quantity))
                    {
                        Console.WriteLine($"[Check] insufficient stock → seller={item.SellerId}, product={item.ProductId}, available={stockItem.qty_available}, reserved={stockItem.qty_reserved}, request={item.Quantity}");
    
                        unavailableItems.Add(new ProductStatus(item.ProductId, ItemStatus.OUT_OF_STOCK, stockItem.qty_available));
                        continue;
                    }

                    stockItem.qty_reserved += item.Quantity;
                    stockItem.updated_at = now;
                    cartItemsReserved.Add(item);
                    stockItemsReserved.Add(stockItem);
                }

                if (cartItemsReserved.Count > 0)
                {
                    stockRepository.UpdateRange(stockItemsReserved);
                    stockRepository.FlushUpdates();
                    txCtx.Commit();
                }

                if (config.Streaming)
                {
                    if (cartItemsReserved.Count > 0)
                    {
                        var stockConfirmed = new StockConfirmed(checkout.timestamp, checkout.customerCheckout, cartItemsReserved, checkout.instanceId);
                        await eventPublisher.PublishEventAsync(nameof(StockConfirmed), stockConfirmed);
                    }

                    if (unavailableItems.Count > 0)
                    {
                        if (config.RaiseStockFailed)
                        {
                            var reserveFailed = new ReserveStockFailed(checkout.timestamp, checkout.customerCheckout, unavailableItems, checkout.instanceId);
                            await eventPublisher.PublishEventAsync(nameof(ReserveStockFailed), reserveFailed);
                        }

                        // Console.WriteLine("[ReserveStock] Reserved count = 0, investigating cause...");

                        // foreach (var item in checkout.items)
                        // {
                        //     Console.WriteLine($"Checking item: sellerId={item.SellerId}, productId={item.ProductId}, version={item.Version}, qty={item.Quantity}");
                        // }

                        if (cartItemsReserved.Count == 0)
                        {
                            logger.LogWarning("No items in checkout were reserved: \n{Checkout}", checkout);
                            await eventPublisher.PublishEventAsync(streamReserveId, new TransactionMark(checkout.instanceId, TransactionType.CUSTOMER_SESSION, checkout.customerCheckout.CustomerId, MarkStatus.NOT_ACCEPTED, "stock"));
                        }
                    }
                }
            }
        }


        public async Task ProcessPoisonReserveStock(ReserveStock reserveStock)
        {
            await eventPublisher.PublishEventAsync(streamReserveId, new TransactionMark(reserveStock.instanceId, TransactionType.CUSTOMER_SESSION, reserveStock.customerCheckout.CustomerId, MarkStatus.ABORT, "stock"));
        }

        public void CancelReservation(PaymentFailed payment)
        {
            var ids = payment.items.Select(p => (p.seller_id, p.product_id)).ToList();
            using (var tx = stockRepository.BeginTransaction())
            {
                var stockItems = stockRepository.GetItems(ids).ToDictionary(p => (p.seller_id, p.product_id));
                var now = DateTime.UtcNow;
                foreach (var item in payment.items)
                {
                    var stockItem = stockItems[(item.seller_id, item.product_id)];
                    stockItem.qty_reserved -= item.quantity;
                    stockItem.updated_at = now;
                    stockRepository.Update(stockItem);
                }
                stockRepository.FlushUpdates();
                tx.Commit();
            }
        }

        public void ConfirmReservation(PaymentConfirmed payment)
        {
            var ids = payment.items.Select(p => (p.seller_id, p.product_id)).ToList();
            using (var tx = stockRepository.BeginTransaction())
            {
                var stockItems = stockRepository.GetItems(ids).ToDictionary(p => (p.seller_id, p.product_id));
                var now = DateTime.UtcNow;
                foreach (var item in payment.items)
                {
                    var stockItem = stockItems[(item.seller_id, item.product_id)];
                    stockItem.qty_available -= item.quantity;
                    stockItem.qty_reserved -= item.quantity;
                    stockItem.order_count++;
                    stockItem.updated_at = now;
                    stockRepository.Update(stockItem);
                }
                stockRepository.FlushUpdates();
                tx.Commit();
            }
        }

        public async Task IncreaseStock(IncreaseStock increaseStock)
        {
            using (var txCtx = stockRepository.BeginTransaction())
            {
                var item = stockRepository.Find(increaseStock.seller_id, increaseStock.product_id);
                if (item is null)
                {
                    logger.LogWarning("Attempt to lock item {0},{1} has not succeeded", increaseStock.seller_id, increaseStock.product_id);
                    throw new Exception("Attempt to lock item " + increaseStock.product_id + " has not succeeded");
                }

                item.qty_available += increaseStock.quantity;
                stockRepository.Update(item);
                txCtx.Commit();

                if (config.Streaming)
                {
                    var stockItem = new StockItem
                    {
                        seller_id = item.seller_id,
                        product_id = item.product_id,
                        qty_available = item.qty_available,
                        qty_reserved = item.qty_reserved,
                        order_count = item.order_count,
                        ytd = item.ytd,
                        data = item.data
                    };

                    await eventPublisher.PublishEventAsync(nameof(StockItem), stockItem);
                }
            }
        }

        public Task CreateStockItem(StockItem stockItem)
        {
            var model = new StockItemModel()
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

            using (var tx = stockRepository.BeginTransaction())
            {
                var existing = stockRepository.Find(stockItem.seller_id, stockItem.product_id);
                if (existing == null)
                    stockRepository.Insert(model);
                else
                    stockRepository.Update(model);

                stockRepository.FlushUpdates();
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        public void Cleanup() => stockRepository.Cleanup();

        public void Reset() => stockRepository.Reset(config.DefaultInventory);
    }
}
