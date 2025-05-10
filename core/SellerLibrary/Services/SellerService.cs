using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.SellerLibrary.DTO;
using OnlineMarket.Core.SellerLibrary.Infra;
using OnlineMarket.Core.SellerLibrary.Models;
using OnlineMarket.Core.SellerLibrary.Repositories;
using OnlineMarket.Core.SellerLibrary.Services;

namespace OnlineMarket.Core.SellerLibrary.Services;

/// <summary>
/// 负责卖家侧订单视图与仪表盘的聚合与维护。  
/// 不直接对外发布事件，仅操作本地仓储并刷新视图表。
/// </summary>
public sealed class SellerServiceCore : ISellerService
{
    private readonly ISellerRepository sellerRepository;
    private readonly SellerConfig       config;
    private readonly ILogger<SellerServiceCore> logger;

    public SellerServiceCore(
        ISellerRepository          sellerRepository,
        SellerConfig               config,
        ILogger<SellerServiceCore> logger)
    {
        this.sellerRepository = sellerRepository;
        this.config           = config;
        this.logger           = logger;
    }

    /* ---------- 订单开票 ---------- */

    /// <inheritdoc/>
    public void ProcessInvoiceIssued(InvoiceIssued invoice)
    {
        using var tx = this.sellerRepository.BeginTransaction();

        foreach (var item in invoice.items)
        {
            var orderEntry = new OrderEntry
            {
                customer_id      = invoice.customer.CustomerId,
                order_id         = invoice.orderId,
                seller_id        = item.seller_id,
                product_id       = item.product_id,
                product_name     = item.product_name,
                product_category = string.Empty,      // 如有需要，可由产品服务反填
                unit_price       = item.unit_price,
                quantity         = item.quantity,
                total_items      = item.total_items,
                total_amount     = item.total_amount,
                total_invoice    = item.total_amount + item.freight_value,
                total_incentive  = item.total_incentive,
                freight_value    = item.freight_value,
                order_status     = OrderStatus.INVOICED,
                natural_key      = $"{invoice.customer.CustomerId}_{invoice.orderId}"
            };
            this.sellerRepository.AddOrderEntry(orderEntry);
        }

        this.sellerRepository.FlushUpdates();
        tx.Commit();

        // 物化视图或同义表异步刷新
        this.sellerRepository.RefreshSellerViewSafely();
    }

    /* ---------- 发货／运输状态 ---------- */

    public void ProcessShipmentNotification(ShipmentNotification note)
    {
        using var tx = this.sellerRepository.BeginTransaction();

        var entries = this.sellerRepository.GetOrderEntries(note.customerId, note.orderId);

        foreach (var entry in entries)
        {
            switch (note.status)
            {
                case ShipmentStatus.approved:
                    entry.order_status   = OrderStatus.READY_FOR_SHIPMENT;
                    entry.shipment_date  = note.eventDate;
                    entry.delivery_status = PackageStatus.ready_to_ship;
                    break;

                case ShipmentStatus.delivery_in_progress:
                    entry.order_status   = OrderStatus.IN_TRANSIT;
                    entry.delivery_status = PackageStatus.shipped;
                    break;

                case ShipmentStatus.concluded:
                    entry.order_status = OrderStatus.DELIVERED;
                    break;
            }
        }

        this.sellerRepository.UpdateRange(entries);
        this.sellerRepository.FlushUpdates();
        tx.Commit();

        this.sellerRepository.RefreshSellerViewSafely();
    }

    /* ---------- 单包裹妥投/异常 ---------- */

    public void ProcessDeliveryNotification(DeliveryNotification delivery)
    {
        using var tx = this.sellerRepository.BeginTransaction();

        var entry = this.sellerRepository.Find(
            delivery.customerId,
            delivery.orderId,
            delivery.sellerId,
            delivery.productId)
            ?? throw new InvalidOperationException(
                $"[DeliveryNotification] Order #{delivery.orderId} / Product {delivery.productId} not found.");

        entry.package_id     = delivery.packageId;
        entry.delivery_date  = delivery.deliveryDate;
        entry.delivery_status = delivery.status;

        this.sellerRepository.Update(entry);
        this.sellerRepository.FlushUpdates();
        tx.Commit();
    }

    /* ---------- 支付结果 ---------- */

    public void ProcessPaymentConfirmed(PaymentConfirmed payment)
        => UpdateOrderStatus(payment.customer.CustomerId, payment.orderId, OrderStatus.PAYMENT_PROCESSED);

    public void ProcessPaymentFailed(PaymentFailed payment)
        => UpdateOrderStatus(payment.customer.CustomerId, payment.orderId, OrderStatus.PAYMENT_FAILED);

    private void UpdateOrderStatus(int customerId, int orderId, OrderStatus status)
    {
        using var tx = this.sellerRepository.BeginTransaction();

        var entries = this.sellerRepository.GetOrderEntries(customerId, orderId);
        foreach (var e in entries) e.order_status = status;

        this.sellerRepository.UpdateRange(entries);
        this.sellerRepository.FlushUpdates();
        tx.Commit();
    }

    /* ---------- 查询 ---------- */

    public SellerDashboard QueryDashboard(int sellerId)
    {
        using var _ = this.sellerRepository.BeginTransaction();
        return this.sellerRepository.QueryDashboard(sellerId);
    }

    /* ---------- 维护 ---------- */

    public void Cleanup() => this.sellerRepository.Cleanup();

    public void Reset()   => this.sellerRepository.Reset();
}
