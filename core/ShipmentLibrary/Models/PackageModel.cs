using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.ShipmentLibrary.Models;

[Table("packages", Schema = "shipment")]
[PrimaryKey(nameof(customer_id), nameof(order_id), nameof(package_id))]
public class PackageModel
{
    public int customer_id { get; set; }
    public int order_id { get; set; }
    public int package_id { get; set; }

    public int seller_id { get; set; }

    public int product_id { get; set; }

    public string product_name { get; set; } = "";

    public float freight_value { get; set; }

    public DateTime shipping_date { get; set; }

    public DateTime? delivery_date { get; set; }

    public int quantity { get; set; }

    public PackageStatus status { get; set; }

    public PackageModel() { }

    public string GetOrderIdAsString()
    {
        return $"{customer_id}|{order_id}";
    }

}

