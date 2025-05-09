using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.CartLibrary.Infra;
using OnlineMarket.Core.CartLibrary.Models;
using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.Common.Infra;

namespace CartMS.Repositories.Impl;

public class InMemoryProductReplicaRepository : IProductReplicaRepository
{
    private readonly ConcurrentDictionary<(int sellerId, int productId),ProductReplicaModel> productReplicas;

    private readonly ILogging logging;

	public InMemoryProductReplicaRepository(IOptions<CartConfig> config)
	{
        this.productReplicas = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "product_replica");
	}

    public bool Exists(int sellerId, int productId)
    {
        return this.productReplicas.ContainsKey((sellerId,productId));
    }

    public ProductReplicaModel GetProduct(int sellerId, int productId)
    {
        return this.productReplicas[(sellerId,productId)];
    }

    public ProductReplicaModel GetProductForUpdate(int sellerId, int productId)
    {
        return this.GetProduct(sellerId, productId);
    }

    public IList<ProductReplicaModel> GetProducts(IList<(int, int)> ids)
    {
        List<ProductReplicaModel> res = new();
        foreach(var id in ids)
        {
            res.Add( this.GetProduct(id.Item1, id.Item2) );
        }
        return res;
    }

    public ProductReplicaModel Insert(ProductReplicaModel product)
    {
        this.productReplicas.TryAdd((product.seller_id, product.product_id), product);
        this.logging.Append(product);
        return product;
    }

    public ProductReplicaModel Update(ProductReplicaModel product)
    {
        this.productReplicas[(product.seller_id, product.product_id)] = product;
        this.logging.Append(product);
        return product;
    }

    public void Reset()
    {
        foreach(var item in this.productReplicas.Values)
        {
            item.active = true;
            item.version = "0";
        }
        this.logging.Clear();
    }

    public void Cleanup()
    {
        this.productReplicas.Clear();
        this.logging.Clear();
    }

}

