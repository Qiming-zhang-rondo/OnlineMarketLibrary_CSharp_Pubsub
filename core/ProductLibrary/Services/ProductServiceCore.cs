using System.Text;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.ProductLibrary.Infra;
using OnlineMarket.Core.ProductLibrary.Models;
using OnlineMarket.Core.ProductLibrary.Repositories;


namespace OnlineMarket.Core.ProductLibrary.Services
{
    public class ProductServiceCore : IProductService
    {
        private const string PUBSUB_NAME = "pubsub";
        private readonly IProductRepository productRepository;
        private readonly IEventPublisher eventPublisher;
        private readonly ProductConfig config;

        private readonly string streamId = new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.PRICE_UPDATE.ToString())
            .ToString();

        public ProductServiceCore(
            IProductRepository productRepository,
            IEventPublisher eventPublisher,
            ProductConfig config)
        {
            this.productRepository = productRepository;
            this.eventPublisher = eventPublisher;
            this.config = config;
        }

        public void ProcessCreateProduct(Product product)
        {
            ProductModel input = Utils.AsProductModel(product);
            using (var txCtx = this.productRepository.BeginTransaction())
            {
                var existing = this.productRepository.GetProduct(product.seller_id, product.product_id);
                if (existing is null)
                    this.productRepository.Insert(input);
                else
                    this.productRepository.Update(input);
                txCtx.Commit();
            }
        }

        public async Task ProcessPriceUpdate(PriceUpdate priceUpdate)
        {
            using (var txCtx = this.productRepository.BeginTransaction())
            {
                var product = this.productRepository.GetProductForUpdate(priceUpdate.sellerId, priceUpdate.productId);

                // check if versions match
                if (product.version.SequenceEqual(priceUpdate.version))
                {
                    product.price = priceUpdate.price;
                    this.productRepository.Update(product);
                    txCtx.Commit();
                }

                if (this.config.Streaming)
                {
                    PriceUpdated update = new(
                        priceUpdate.sellerId,
                        priceUpdate.productId,
                        priceUpdate.price,
                        priceUpdate.version,
                        priceUpdate.instanceId
                    );
                    await this.eventPublisher.PublishEventAsync(nameof(PriceUpdated), update);
                }
            }
        }

        public async Task ProcessPoisonPriceUpdate(PriceUpdate priceUpdate)
        {
            if (this.config.Streaming)
            {
                var transactionMark = new TransactionMark(
                    priceUpdate.instanceId,
                    TransactionType.PRICE_UPDATE,
                    priceUpdate.sellerId,
                    MarkStatus.ERROR,
                    "product"
                );
                await this.eventPublisher.PublishEventAsync(streamId, transactionMark);
            }
        }

        public async Task ProcessProductUpdate(Product product)
        {
            using (var txCtx = this.productRepository.BeginTransaction())
            {
                var oldProduct = this.productRepository.GetProductForUpdate(product.seller_id, product.product_id);
                if (oldProduct is null)
                {
                    throw new ApplicationException("Product not found " + product.seller_id + "-" + product.product_id);
                }
                ProductModel input = Utils.AsProductModel(product);

                this.productRepository.Update(input);

                txCtx.Commit();
                if (this.config.Streaming)
                {
                    ProductUpdated productUpdated = new(
                        input.seller_id,
                        input.product_id,
                        input.name,
                        input.sku,
                        input.category,
                        input.description,
                        input.price,
                        input.freight_value,
                        input.status,
                        input.version
                    );
                    await this.eventPublisher.PublishEventAsync(nameof(ProductUpdated), productUpdated);
                }
            }
        }

        public async Task ProcessPoisonProductUpdate(Product product)
        {
            if (this.config.Streaming)
            {
                var transactionMark = new TransactionMark(
                    product.version,
                    TransactionType.PRICE_UPDATE,
                    product.seller_id,
                    MarkStatus.ERROR,
                    "product"
                );
                await this.eventPublisher.PublishEventAsync(streamId, transactionMark);
            }
        }

        public void Cleanup()
        {
            this.productRepository.Cleanup();
        }

        public void Reset()
        {
            this.productRepository.Reset();
        }
    }
}