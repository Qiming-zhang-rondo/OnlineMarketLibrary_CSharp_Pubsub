using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.SellerLibrary.DTO;
using OnlineMarket.Core.SellerLibrary.Infra;
using OnlineMarket.Core.SellerLibrary.Models;
using OnlineMarket.Core.SellerLibrary.Repositories;

namespace SellerMS.Repositories;

public class InMemorySellerRepository : ISellerRepository
{
    private readonly ConcurrentDictionary<int, SellerModel> sellers;
    private readonly ConcurrentDictionary<(int customerId, int orderId, int sellerId, int productId), OrderEntry> orderEntries;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

    public InMemorySellerRepository(IOptions<SellerConfig> config)
    {
        this.sellers = new();
        this.orderEntries = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "seller");
    }

    public SellerModel Insert(SellerModel seller)
    {
        this.sellers.TryAdd(seller.id, seller);
        this.logging.Append(seller);
        return seller;
    }

	public SellerModel? Get(int sellerId)
    {
        if(this.sellers.ContainsKey(sellerId))
            return this.sellers[sellerId];
        return null;
    }

	public void RefreshSellerViewSafely()
    {
        // do nothing
    }

	public SellerDashboard QueryDashboard(int sellerId)
    {
        var entries = this.orderEntries.Values.Where(oe => oe.seller_id == sellerId && (oe.order_status == OrderStatus.INVOICED || oe.order_status == OrderStatus.READY_FOR_SHIPMENT || oe.order_status == OrderStatus.IN_TRANSIT || oe.order_status == OrderStatus.PAYMENT_PROCESSED)).ToList();
        
        OrderSellerView view = new OrderSellerView()
        {
            seller_id = sellerId,
            count_orders = entries.Select(x => (x.customer_id, x.order_id)).ToHashSet().Count,
            count_items = entries.Count(),
            total_invoice = entries.Sum(x => x.total_invoice),
            total_amount = entries.Sum(x => x.total_amount),
            total_freight = entries.Sum(x => x.freight_value),
            total_incentive = entries.Sum(x => x.total_incentive),
            total_items = entries.Sum(x => x.total_items),
        };
        return new SellerDashboard(view, entries);   
    }

	public void AddOrderEntry(OrderEntry orderEntry)
    {
        this.orderEntries.TryAdd((orderEntry.customer_id, orderEntry.order_id, orderEntry.seller_id, orderEntry.product_id), orderEntry);
        this.logging.Append(orderEntry);
    }

	public void Update(OrderEntry orderEntry)
    {
        this.orderEntries[(orderEntry.customer_id, orderEntry.order_id, orderEntry.seller_id, orderEntry.product_id)] = orderEntry;
        this.logging.Append(orderEntry);
    }

	public void UpdateRange(IEnumerable<OrderEntry> orderEntries)
    {
        foreach(var entry in orderEntries)
        {
            this.Update(entry);         
        }
    }

	public IEnumerable<OrderEntry> GetOrderEntries(int customerId, int orderId)
    {
        return this.orderEntries.Values.Where(oe =>
                oe.customer_id == customerId &&
                oe.order_id == orderId);
    }

	public OrderEntry? Find(int customerId, int orderId, int sellerId, int productId)
    {
        if(this.orderEntries.ContainsKey((customerId, orderId, sellerId, productId)))
            return this.orderEntries[(customerId, orderId, sellerId, productId)];
        return null;
    }

    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
    }

	public void FlushUpdates()
    {
        // do nothing
    }

    public void Reset()
    {
        this.orderEntries.Clear();
        this.logging.Clear();
    }

    public void Cleanup()
    {
        this.sellers.Clear();
        this.orderEntries.Clear();
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