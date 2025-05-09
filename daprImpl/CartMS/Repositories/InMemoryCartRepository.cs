using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.CartLibrary.Infra;
using OnlineMarket.Core.CartLibrary.Models;
using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.Common.Infra;

namespace CartMS.Repositories.Impl;

public class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<(int customerId, int sellerId, int productId),CartItemModel> cartItems;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryCartRepository(IOptions<CartConfig> config)
	{
        this.cartItems = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "cart");
	}

    // CART
    public void Insert(CartModel cart)
    {
        // do nothing
        return;
    }

    public void Update(CartModel cart)
    {
        // do nothing
        return;
    }

    public CartModel? Delete(int customerId)
    {
        var items = this.GetItems(customerId);
        foreach(var item in items)
        {
            this.cartItems.Remove( (item.customer_id, item.seller_id, item.product_id), out var deletedItem );
            if(deletedItem is not null)
                this.logging.Append(deletedItem);
        }
        return null;
    }

    public CartModel? GetCart(int customerId)
    {
        // do nothing
        return null;
    }

    // ITEMS
    public CartItemModel AddItem(CartItemModel item)
    {
        this.cartItems.TryAdd((item.customer_id, item.seller_id, item.product_id), item);
        return item;
    }

    public CartItemModel UpdateItem(CartItemModel item)
    {
        this.cartItems[ (item.customer_id, item.seller_id, item.product_id) ] = item;
        return item;
    }

    public IList<CartItemModel> GetItems(int customerId)
    {
        return this.cartItems.Values.Where(c=>c.customer_id == customerId).ToList();
    }

    public IList<CartItemModel> GetItemsByProduct(int sellerId, int productId, string version)
    {
        return this.cartItems.Values.Where(i=> i.seller_id == sellerId && i.product_id == productId && i.version.SequenceEqual(version)).ToList();
    }

    // DB
    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
    }

    public void FlushUpdates()
    {
        // do nothing
    }

    public void Cleanup()
    {
        this.cartItems.Clear();
        this.logging.Clear();
    }

    public void Reset()
    {
        this.cartItems.Clear();
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

