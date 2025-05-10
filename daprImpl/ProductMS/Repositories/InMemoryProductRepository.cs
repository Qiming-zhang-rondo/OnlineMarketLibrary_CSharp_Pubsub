using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Infra;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.ProductLibrary.Repositories;
using OnlineMarket.Core.ProductLibrary.Models;
using OnlineMarket.Core.ProductLibrary.Infra;


namespace ProductMS.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<(int sellerId, int productId),ProductModel> products;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryProductRepository(IOptions<ProductConfig> config)
	{
        this.products = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "product");
	}

    public IEnumerable<ProductModel> GetBySeller(int sellerId)
    {
        return this.products.Values.Where(i=>i.seller_id == sellerId);
    }

    public ProductModel? GetProduct(int sellerId, int productId)
    {
        if (this.products.ContainsKey((sellerId, productId)))
        {
            return this.products[(sellerId, productId)];
        }
        return null;
    }

    public ProductModel GetProductForUpdate(int sellerId, int productId)
    {
        var product = this.GetProduct(sellerId,productId);
        if(product is null) throw new ApplicationException($"Cannot find product {sellerId}-{productId}");
        return product;
    }

    public void Insert(ProductModel product)
    {
        product.created_at = DateTime.UtcNow;
        product.updated_at = product.created_at;
        product.active = true;
        this.products.TryAdd( (product.seller_id, product.product_id),  product);
        this.logging.Append(product);
    }

    public void Update(ProductModel product)
    {
        // keep the old
        product.created_at = this.products[(product.seller_id, product.product_id)].created_at;
        product.updated_at = DateTime.UtcNow;
        product.active = true;
        this.products[(product.seller_id, product.product_id)] = product;
        this.logging.Append(product);
    }

    public void Reset()
    {
        foreach(var item in this.products.Values)
        {
            item.active = true;
            item.version = "0";
        }
        this.logging.Clear();
    }

    public void Cleanup()
    {
        this.products.Clear();
        this.logging.Clear();
    }

    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
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

