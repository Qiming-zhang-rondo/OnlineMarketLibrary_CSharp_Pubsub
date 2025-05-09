using System.Text;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.CartLibrary.Models;
using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.CartLibrary.Services;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.CartLibrary.Infra;

namespace OnlineMarket.Core.CartLibrary.Services;

public class CartServiceCore : ICartService
{
    private readonly ICartRepository cartRepository;
    private readonly IProductReplicaRepository productReplicaRepository;
    private readonly IEventPublisher eventPublisher;
    private readonly CartConfig config;
    private readonly ILogger<CartServiceCore> logger;

    public CartServiceCore(
        ICartRepository cartRepository,
        IProductReplicaRepository productReplicaRepository,
        IEventPublisher eventPublisher,
        CartConfig config,
        ILogger<CartServiceCore> logger)
    {
        this.cartRepository = cartRepository;
        this.productReplicaRepository = productReplicaRepository;
        this.eventPublisher = eventPublisher;
        this.config = config;
        this.logger = logger;
    }

    static readonly string CheckoutStreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.CUSTOMER_SESSION.ToString())
            .ToString();

    static readonly string PriceUpdateStreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.PRICE_UPDATE.ToString())
            .ToString();

    public void Seal(CartModel cart, bool cleanItems = true)
    {
        cart.status = CartStatus.OPEN;
        if (cleanItems)
        {
            this.cartRepository.Delete(cart.customer_id);
        }
        cart.updated_at = DateTime.UtcNow;
        this.cartRepository.Update(cart);
    }

    public async Task NotifyCheckout(CustomerCheckout customerCheckout)
    {
        using (var txCtx = this.cartRepository.BeginTransaction())
        {
            List<CartItem> cartItems;

            if (config.ControllerChecks)
            {
                IList<CartItemModel> items = GetItemsWithoutDivergencies(customerCheckout.CustomerId);
                if (items.Count == 0)
                {
                    throw new ApplicationException($"Cart {customerCheckout.CustomerId} has no valid items");
                }

                var cart = this.cartRepository.GetCart(customerCheckout.CustomerId);
                if (cart == null)
                {
                    throw new ApplicationException($"Cart {customerCheckout.CustomerId} not found");
                }

                cart.status = CartStatus.CHECKOUT_SENT;
                this.cartRepository.Update(cart);

                cartItems = items.Select(i => new CartItem()
                {
                    SellerId = i.seller_id,
                    ProductId = i.product_id,
                    ProductName = i.product_name ?? "",
                    UnitPrice = i.unit_price,
                    FreightValue = i.freight_value,
                    Quantity = i.quantity,
                    Version = i.version,
                    Voucher = i.voucher
                }).ToList();

                this.Seal(cart);
            }
            else
            {
                IList<CartItemModel> cartItemModels = this.cartRepository.GetItems(customerCheckout.CustomerId);
                this.cartRepository.Delete(customerCheckout.CustomerId);
                cartItems = cartItemModels.Select(i => new CartItem()
                {
                    SellerId = i.seller_id,
                    ProductId = i.product_id,
                    ProductName = i.product_name ?? "",
                    UnitPrice = i.unit_price,
                    FreightValue = i.freight_value,
                    Quantity = i.quantity,
                    Version = i.version,
                    Voucher = i.voucher
                }).ToList();
            }

            txCtx.Commit();

            if (this.config.Streaming)
            {
                var checkoutEvent = new ReserveStock(DateTime.UtcNow, customerCheckout, cartItems, customerCheckout.instanceId);
                await this.eventPublisher.PublishEventAsync(nameof(ReserveStock), checkoutEvent);
            }
        }
    }

    public async Task ProcessPoisonCheckout(CustomerCheckout customerCheckout, MarkStatus status)
    {
        var transactionMark = new TransactionMark(
            customerCheckout.instanceId,
            TransactionType.CUSTOMER_SESSION,
            customerCheckout.CustomerId,
            status,
            "cart"
        );

        if (this.config.Streaming)
        {
            await this.eventPublisher.PublishEventAsync(CheckoutStreamId, transactionMark);
        }
    }

    private List<CartItemModel> GetItemsWithoutDivergencies(int customerId)
    {
        var items = cartRepository.GetItems(customerId);
        var itemsDict = items.ToDictionary(i => (i.seller_id, i.product_id));

        var ids = items.Select(i => (i.seller_id, i.product_id)).ToList();
        IList<ProductReplicaModel> products = productReplicaRepository.GetProducts(ids);

        foreach (var product in products)
        {
            var item = itemsDict[(product.seller_id, product.product_id)];
            var currPrice = item.unit_price;

            if (item.version.Equals(product.version) && currPrice != product.price)
            {
                itemsDict.Remove((product.seller_id, product.product_id));
            }
        }
        return itemsDict.Values.ToList();
    }

    public async Task ProcessPriceUpdate(PriceUpdated priceUpdate)
    {
        using (var txCtx = this.cartRepository.BeginTransaction())
        {
            if (this.productReplicaRepository.Exists(priceUpdate.seller_id, priceUpdate.product_id))
            {
                ProductReplicaModel product = this.productReplicaRepository.GetProductForUpdate(priceUpdate.seller_id, priceUpdate.product_id);

                if (product.version.SequenceEqual(priceUpdate.version))
                {
                    product.price = priceUpdate.price;
                    this.productReplicaRepository.Update(product);
                }
            }

            var cartItems = this.cartRepository.GetItemsByProduct(priceUpdate.seller_id, priceUpdate.product_id, priceUpdate.version);
            foreach (var item in cartItems)
            {
                item.unit_price = priceUpdate.price;
                item.voucher += priceUpdate.price - item.unit_price;
            }

            txCtx.Commit();
        }

        if (this.config.Streaming)
        {
            var transactionMark = new TransactionMark(
                priceUpdate.instanceId,
                TransactionType.PRICE_UPDATE,
                priceUpdate.seller_id,
                MarkStatus.SUCCESS,
                "cart"
            );

            await this.eventPublisher.PublishEventAsync(PriceUpdateStreamId, transactionMark);
        }
    }

    public async Task ProcessPoisonPriceUpdate(PriceUpdated productUpdate)
    {
        var transactionMark = new TransactionMark(
            productUpdate.instanceId,
            TransactionType.PRICE_UPDATE,
            productUpdate.seller_id,
            MarkStatus.ABORT,
            "cart"
        );

        await this.eventPublisher.PublishEventAsync(PriceUpdateStreamId, transactionMark);
    }

    public void ProcessProductUpdated(ProductUpdated productUpdated)
    {
        ProductReplicaModel product = new()
        {
            seller_id = productUpdated.seller_id,
            product_id = productUpdated.product_id,
            name = productUpdated.name,
            price = productUpdated.price,
            version = productUpdated.version
        };

        if (this.productReplicaRepository.Exists(productUpdated.seller_id, productUpdated.product_id))
        {
            this.productReplicaRepository.Update(product);
        }
        else
        {
            this.productReplicaRepository.Insert(product);
        }
    }

    public async Task ProcessPoisonProductUpdated(ProductUpdated productUpdate)
    {
        var transactionMark = new TransactionMark(
            productUpdate.version,
            TransactionType.UPDATE_PRODUCT,
            productUpdate.seller_id,
            MarkStatus.ABORT,
            "cart"
        );

        await this.eventPublisher.PublishEventAsync(PriceUpdateStreamId, transactionMark);
    }

    public void Cleanup()
    {
        this.cartRepository.Cleanup();
        this.productReplicaRepository.Cleanup();
    }

    public void Reset()
    {
        this.cartRepository.Reset();
        this.productReplicaRepository.Reset();
    }
}