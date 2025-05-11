using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.ShipmentLibrary.Models;

[Table("shipments", Schema = "shipment")]
[PrimaryKey(nameof(customer_id), nameof(order_id))]
public class ShipmentModel
{
    public int customer_id { get; set; }
    public int order_id { get; set; }

    public int package_count { get; set; }
    public float total_freight_value { get; set; }

    public DateTime request_date { get; set; }

    public ShipmentStatus status { get; set; }

    // customer
    public string first_name { get; set; }

    public string last_name { get; set; }

    public string street { get; set; }

    public string complement { get; set; }

    public string zip_code { get; set; }

    public string city { get; set; }

    public string state { get; set; }

    [ForeignKey("customer_id, order_id")]
    public ICollection<PackageModel> packages { get; } = new List<PackageModel>();

    public ShipmentModel()
    {
    }

}
