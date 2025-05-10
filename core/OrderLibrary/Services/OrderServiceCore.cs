using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.OrderLibrary.Models;
using OnlineMarket.Core.OrderLibrary.Repositories;
using OnlineMarket.Core.OrderLibrary.Infra;
using OnlineMarket.Common.Messaging;

namespace OnlineMarket.Core.OrderLibrary.Services;

public class OrderServiceCore : IOrderService
{
    private static readonly CultureInfo enUS = CultureInfo.CreateSpecificCulture("en-US");
    private static readonly DateTimeFormatInfo dtfi = enUS.DateTimeFormat;

    static OrderServiceCore()
    {
        dtfi.ShortDatePattern = "yyyyMMdd";
    }

    private readonly IOrderRepository orderRepository;
    private readonly IEventPublisher eventPublisher;
    private readonly OrderConfig config;
    private readonly ILogger<OrderServiceCore> logger;

    private static readonly string StreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.CUSTOMER_SESSION.ToString())
            .ToString();

    public OrderServiceCore(
        IOrderRepository orderRepository,
        IEventPublisher eventPublisher,
        IOptions<OrderConfig> config,
        ILogger<OrderServiceCore> logger)
    {
        this.orderRepository = orderRepository;
        this.eventPublisher = eventPublisher;
        this.config = config.Value;
        this.logger = logger;
    }

    public async Task ProcessStockConfirmed(StockConfirmed checkout)
    {
        using (var txCtx = this.orderRepository.BeginTransaction())
        {
            var now = DateTime.UtcNow;
            float total_freight = 0;
            float total_amount = 0;
            foreach (var item in checkout.items)
            {
                total_freight += item.FreightValue;
                total_amount += (item.UnitPrice * item.Quantity);
            }

            float total_items = total_amount;

            Dictionary<(int, int), float> totalPerItem = new();
            float total_incentive = 0;
            foreach (var item in checkout.items)
            {
                float total_item = item.UnitPrice * item.Quantity;

                if (total_item - item.Voucher > 0)
                {
                    total_amount -= item.Voucher;
                    total_incentive += item.Voucher;
                    total_item -= item.Voucher;
                }
                else
                {
                    total_amount -= total_item;
                    total_incentive += total_item;
                    total_item = 0;
                }

                totalPerItem.Add((item.SellerId, item.ProductId), total_item);
            }

            var customerOrder = this.orderRepository.GetCustomerOrderByCustomerId(checkout.customerCheckout.CustomerId);
            if (customerOrder is null)
            {
                customerOrder = new()
                {
                    customer_id = checkout.customerCheckout.CustomerId,
                    next_order_id = 1
                };
                customerOrder = this.orderRepository.InsertCustomerOrder(customerOrder);
            }
            else
            {
                customerOrder.next_order_id += 1;
                customerOrder = this.orderRepository.UpdateCustomerOrder(customerOrder);
            }

            StringBuilder stringBuilder = new StringBuilder()
                .Append(checkout.customerCheckout.CustomerId)
                .Append('-').Append(now.ToString("d", enUS))
                .Append('-').Append(customerOrder.next_order_id);

            OrderModel newOrder = new()
            {
                customer_id = checkout.customerCheckout.CustomerId,
                order_id = customerOrder.next_order_id,
                invoice_number = stringBuilder.ToString(),
                status = OrderStatus.INVOICED,
                purchase_date = checkout.timestamp,
                total_amount = total_amount,
                total_items = total_items,
                total_freight = total_freight,
                total_incentive = total_incentive,
                total_invoice = total_amount + total_freight,
                count_items = checkout.items.Count(),
                created_at = now,
                updated_at = now
            };
            var orderPersisted = this.orderRepository.InsertOrder(newOrder);
            this.orderRepository.FlushUpdates();

            List<OrderItem> orderItems = new(checkout.items.Count);

            int id = 1;
            foreach (var item in checkout.items)
            {
                OrderItemModel oim = new()
                {
                    customer_id = checkout.customerCheckout.CustomerId,
                    order_id = customerOrder.next_order_id,
                    order_item_id = id,
                    product_id = item.ProductId,
                    product_name = item.ProductName,
                    seller_id = item.SellerId,
                    unit_price = item.UnitPrice,
                    quantity = item.Quantity,
                    total_items = item.UnitPrice * item.Quantity,
                    total_amount = totalPerItem[(item.SellerId, item.ProductId)],
                    freight_value = item.FreightValue,
                    shipping_limit_date = now.AddDays(3)
                };

                this.orderRepository.InsertOrderItem(oim);

                orderItems.Add(AsOrderItem(oim, item.Voucher));
                id++;
            }

            this.orderRepository.InsertOrderHistory(new OrderHistoryModel()
            {
                customer_id = orderPersisted.customer_id,
                order_id = orderPersisted.order_id,
                created_at = newOrder.created_at,
                status = OrderStatus.INVOICED,
                order = orderPersisted
            });

            this.orderRepository.FlushUpdates();
            txCtx.Commit();

            if (this.config.Streaming)
            {
                InvoiceIssued invoice = new InvoiceIssued(
                    checkout.customerCheckout,
                    customerOrder.next_order_id,
                    newOrder.invoice_number,
                    now,
                    newOrder.total_invoice,
                    orderItems,
                    checkout.instanceId
                );

                await this.eventPublisher.PublishEventAsync(nameof(InvoiceIssued), invoice);
            }
        }
    }

    public async Task ProcessPoisonStockConfirmed(StockConfirmed stockConfirmed)
    {
        var transactionMark = new TransactionMark(
            stockConfirmed.instanceId,
            TransactionType.CUSTOMER_SESSION,
            stockConfirmed.customerCheckout.CustomerId,
            MarkStatus.ABORT,
            "order"
        );

        await this.eventPublisher.PublishEventAsync(StreamId, transactionMark);
    }

    public void ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        var now = DateTime.UtcNow;
        using (var txCtx = this.orderRepository.BeginTransaction())
        {
            OrderModel? order = this.orderRepository.GetOrder(paymentConfirmed.customer.CustomerId, paymentConfirmed.orderId);
            if (order is null)
            {
                throw new Exception($"Cannot find order {paymentConfirmed.customer.CustomerId}-{paymentConfirmed.orderId}");
            }

            order.status = OrderStatus.PAYMENT_PROCESSED;
            order.payment_date = paymentConfirmed.date;
            order.updated_at = now;

            this.orderRepository.UpdateOrder(order);
            this.orderRepository.InsertOrderHistory(new OrderHistoryModel()
            {
                order_id = paymentConfirmed.orderId,
                created_at = now,
                status = OrderStatus.PAYMENT_PROCESSED,
                order = order
            });

            this.orderRepository.FlushUpdates();
            txCtx.Commit();
        }
    }

    public void ProcessPaymentFailed(PaymentFailed paymentFailed)
    {
        var now = DateTime.UtcNow;
        using (var txCtx = this.orderRepository.BeginTransaction())
        {
            OrderModel? order = this.orderRepository.GetOrder(paymentFailed.customer.CustomerId, paymentFailed.orderId);
            if (order is null)
            {
                throw new Exception($"Cannot find order {paymentFailed.customer.CustomerId}-{paymentFailed.orderId}");
            }

            order.status = OrderStatus.PAYMENT_FAILED;
            order.updated_at = now;

            this.orderRepository.UpdateOrder(order);
            this.orderRepository.InsertOrderHistory(new OrderHistoryModel()
            {
                order_id = paymentFailed.orderId,
                created_at = now,
                status = OrderStatus.PAYMENT_FAILED,
                order = order
            });

            this.orderRepository.FlushUpdates();
            txCtx.Commit();
        }
    }

    public void ProcessShipmentNotification(ShipmentNotification shipmentNotification)
    {
        DateTime now = DateTime.UtcNow;
        using (var txCtx = this.orderRepository.BeginTransaction())
        {
            OrderModel? order = this.orderRepository.GetOrder(shipmentNotification.customerId, shipmentNotification.orderId);
            if (order is null)
            {
                throw new Exception($"Cannot find order {shipmentNotification.customerId}-{shipmentNotification.orderId}");
            }

            OrderStatus orderStatus = shipmentNotification.status switch
            {
                ShipmentStatus.delivery_in_progress => OrderStatus.IN_TRANSIT,
                ShipmentStatus.concluded => OrderStatus.DELIVERED,
                _ => OrderStatus.READY_FOR_SHIPMENT
            };

            if (orderStatus == OrderStatus.IN_TRANSIT)
            {
                order.delivered_carrier_date = shipmentNotification.eventDate;
            }
            if (orderStatus == OrderStatus.DELIVERED)
            {
                order.delivered_customer_date = shipmentNotification.eventDate;
            }

            order.status = orderStatus;
            order.updated_at = now;

            this.orderRepository.UpdateOrder(order);
            this.orderRepository.InsertOrderHistory(new OrderHistoryModel()
            {
                order_id = shipmentNotification.orderId,
                created_at = now,
                status = orderStatus,
                order = order
            });

            this.orderRepository.FlushUpdates();
            txCtx.Commit();
        }
    }

    public void Cleanup()
    {
        this.orderRepository.Cleanup();
    }

    private static OrderItem AsOrderItem(OrderItemModel orderItem, float voucher)
    {
        return new()
        {
            order_id = orderItem.order_id,
            order_item_id = orderItem.order_item_id,
            product_id = orderItem.product_id,
            product_name = orderItem.product_name,
            seller_id = orderItem.seller_id,
            unit_price = orderItem.unit_price,
            quantity = orderItem.quantity,
            total_items = orderItem.total_items,
            total_amount = orderItem.total_amount,
            shipping_limit_date = orderItem.shipping_limit_date,
            freight_value = orderItem.freight_value,
            total_incentive = voucher
        };
    }
}