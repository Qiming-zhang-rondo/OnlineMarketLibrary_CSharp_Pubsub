using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Common.Messaging;
using OnlineMarket.Core.ShipmentLibrary.Infra;
using OnlineMarket.Core.ShipmentLibrary.Models;
using OnlineMarket.Core.ShipmentLibrary.Repositories;
using OnlineMarket.Core.ShipmentLibrary.Services;

namespace OnlineMarket.Core.ShipmentLibrary.Services;

public sealed class ShipmentServiceCore : IShipmentService
{
    private readonly IShipmentRepository shipmentRepository;
    private readonly IPackageRepository  packageRepository;
    private readonly IEventPublisher     eventPublisher;
    private readonly ShipmentConfig      config;
    private readonly ILogger<ShipmentServiceCore> logger;

    public ShipmentServiceCore(
        IShipmentRepository         shipmentRepository,
        IPackageRepository          packageRepository,
        IEventPublisher             eventPublisher,
        ShipmentConfig              config,
        ILogger<ShipmentServiceCore> logger)
    {
        this.shipmentRepository = shipmentRepository;
        this.packageRepository  = packageRepository;
        this.eventPublisher     = eventPublisher;
        this.config             = config;
        this.logger             = logger;
    }

    /* ---------- 静态流 ID ---------- */

    private static readonly string CustomerSessionStreamId =
        new StringBuilder(nameof(TransactionMark))
            .Append('_')
            .Append(TransactionType.CUSTOMER_SESSION.ToString())
            .ToString();

    /* ---------- 建立发运 ---------- */

    /// <summary>
    /// 收到付款确认后创建 <see cref="ShipmentModel"/> 与对应包裹，并发布 <see cref="ShipmentNotification"/>。
    /// </summary>
    public async Task ProcessShipment(PaymentConfirmed payment)
    {
        var now = DateTime.UtcNow;

        using (var tx = this.shipmentRepository.BeginTransaction())
        {
            // 创建发运记录
            var shipment = new ShipmentModel
            {
                order_id            = payment.orderId,
                customer_id         = payment.customer.CustomerId,
                package_count       = payment.items.Count,
                total_freight_value = payment.items.Sum(i => i.freight_value),
                request_date        = now,
                status              = ShipmentStatus.approved,
                first_name          = payment.customer.FirstName,
                last_name           = payment.customer.LastName,
                street              = payment.customer.Street,
                complement          = payment.customer.Complement,
                zip_code            = payment.customer.ZipCode,
                city                = payment.customer.City,
                state               = payment.customer.State
            };
            this.shipmentRepository.Insert(shipment);

            // 创建包裹
            var pkgs = new List<PackageModel>();
            int pkgId = 1;
            foreach (var item in payment.items)
            {
                pkgs.Add(new PackageModel
                {
                    customer_id   = payment.customer.CustomerId,
                    order_id      = payment.orderId,
                    package_id    = pkgId++,
                    status        = PackageStatus.shipped,
                    freight_value = item.freight_value,
                    shipping_date = now,
                    seller_id     = item.seller_id,
                    product_id    = item.product_id,
                    product_name  = item.product_name,
                    quantity      = item.quantity
                });
            }

            this.packageRepository.InsertAll(pkgs);
            this.packageRepository.Save();   // 与 ShipmentRepository 共用同一 DbContext

            tx.Commit();
        }

        if (!this.config.Streaming) return;

        // 发运成功事件
        var notification = new ShipmentNotification(
            payment.customer.CustomerId,
            payment.orderId,
            now,
            payment.instanceId,
            ShipmentStatus.approved);

        var mark = new TransactionMark(
            payment.instanceId,
            TransactionType.CUSTOMER_SESSION,
            payment.customer.CustomerId,
            MarkStatus.SUCCESS,
            "shipment");

        await Task.WhenAll(
            this.eventPublisher.PublishEventAsync(nameof(ShipmentNotification), notification),
            this.eventPublisher.PublishEventAsync(CustomerSessionStreamId, mark));
    }

    /* ---------- 毒丸消息 ---------- */

    public async Task ProcessPoisonShipment(PaymentConfirmed payment)
    {
        var mark = new TransactionMark(
            payment.instanceId,
            TransactionType.CUSTOMER_SESSION,
            payment.customer.CustomerId,
            MarkStatus.ABORT,
            "shipment");

        await this.eventPublisher.PublishEventAsync(CustomerSessionStreamId, mark);
    }

    /* ---------- 批量更新包裹投递状态 ---------- */

    public async Task UpdateShipment(string instanceId)
    {
        using var tx = this.shipmentRepository.BeginTransaction(IsolationLevel.Serializable);

        // 取每个卖家最早待投递的 (customerId, orderId) 组合
        var oldestOpenBySeller = this.packageRepository.GetOldestOpenShipmentPerSeller();

        foreach (var (sellerId, ids) in oldestOpenBySeller)
        {
            var customerId = int.Parse(ids[0]);
            var orderId    = int.Parse(ids[1]);

            var packages = this.packageRepository
                            .GetShippedPackagesByOrderAndSeller(customerId, orderId, sellerId)
                            .ToList();

            if (packages.Count == 0)
            {
                this.logger.LogWarning("No packages found for seller {Seller}", sellerId);
                continue;
            }

            await UpdatePackageDelivery(packages, instanceId);
        }

        tx.Commit();
    }

    /* ---------- 私有：包裹投递 ---------- */

    private async Task UpdatePackageDelivery(IEnumerable<PackageModel> sellerPackages, string instanceId)
    {
        var first     = sellerPackages.First();
        int customerId = first.customer_id;
        int orderId    = first.order_id;

        var shipment = this.shipmentRepository.GetById((customerId, orderId))
                     ?? throw new InvalidOperationException($"Shipment ({customerId},{orderId}) not found.");

        var now = DateTime.UtcNow;
        var tasks = new List<Task>();

        /* --- 若第一次投递则更新 Shipment 状态为 “运输中” --- */
        if (shipment.status == ShipmentStatus.approved)
        {
            shipment.status = ShipmentStatus.delivery_in_progress;
            this.shipmentRepository.Update(shipment);
            this.shipmentRepository.Save();

            tasks.Add(this.eventPublisher.PublishEventAsync(
                nameof(ShipmentNotification),
                new ShipmentNotification(customerId, orderId, now, instanceId, ShipmentStatus.delivery_in_progress)));
        }

        /* --- 标记本次批量包裹为已送达 --- */
        foreach (var pkg in sellerPackages)
        {
            pkg.status       = PackageStatus.delivered;
            pkg.delivery_date = now;
            this.packageRepository.Update(pkg);

            tasks.Add(this.eventPublisher.PublishEventAsync(
                nameof(DeliveryNotification),
                new DeliveryNotification(customerId, orderId, pkg.package_id,
                    pkg.seller_id, pkg.product_id, pkg.product_name,
                    PackageStatus.delivered, now, instanceId)));
        }
        this.packageRepository.Save();

        /* --- 判断整单是否全部完成 --- */
        int deliveredBefore = this.packageRepository
                              .GetTotalDeliveredPackagesForOrder(customerId, orderId);
        int deliveredNow    = sellerPackages.Count();
        if (shipment.package_count == deliveredBefore + deliveredNow)
        {
            shipment.status = ShipmentStatus.concluded;
            this.shipmentRepository.Update(shipment);
            this.shipmentRepository.Save();

            tasks.Add(this.eventPublisher.PublishEventAsync(
                nameof(ShipmentNotification),
                new ShipmentNotification(customerId, orderId, now, instanceId, ShipmentStatus.concluded)));
        }

        await Task.WhenAll(tasks);
    }

    /* ---------- 维护 ---------- */

    public void Cleanup() => this.shipmentRepository.Cleanup();

    // public void Reset()   => this.shipmentRepository.Reset();   // 若有默认数据，可扩展
}
