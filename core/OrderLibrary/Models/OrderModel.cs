using System.ComponentModel.DataAnnotations.Schema;
using OnlineMarket.Core.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace OnlineMarket.Core.OrderLibrary.Models
{
    [Table("orders", Schema = "order")]
    [PrimaryKey(nameof(customer_id), nameof(order_id))]
    [Index(nameof(customer_id), IsUnique = false)]
    public class OrderModel
	{
        public int customer_id { get; set; }

        public int order_id { get; set; }

        // https://finom.co/en-fr/blog/invoice-number/
        public string invoice_number { get; set; } = "";

        public OrderStatus status { get; set; } = OrderStatus.CREATED;

        public DateTime purchase_date { get; set; }

        public DateTime? payment_date { get; set; }

        public DateTime? delivered_carrier_date { get; set; }

        public DateTime? delivered_customer_date { get; set; }

        public DateTime? estimated_delivery_date { get; set; }

        public int count_items { get; set; }

        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }

        public float total_amount { get; set; } = 0;
        public float total_freight { get; set; } = 0;
        public float total_incentive { get; set; } = 0;
        public float total_invoice { get; set; } = 0;
        public float total_items { get; set; } = 0;

        public virtual ICollection<OrderItemModel> items { get; } = new List<OrderItemModel>();

        public virtual ICollection<OrderHistoryModel> history { get; } = new List<OrderHistoryModel>();

        public OrderModel() { }

	}
}

