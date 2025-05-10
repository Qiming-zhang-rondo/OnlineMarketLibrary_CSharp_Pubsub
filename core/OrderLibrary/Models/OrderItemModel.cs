using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

/*
 * More info in https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
 * 
 */
namespace OnlineMarket.Core.OrderLibrary.Models
{
    [Table("order_items", Schema = "order")]
    [PrimaryKey(nameof(customer_id), nameof(order_id), nameof(order_item_id))]
    public class OrderItemModel
	{
        public int customer_id { get; set; }

        public int order_id { get; set; }

        public int order_item_id { get; set; }

        public int product_id { get; set; }

        public string product_name { get; set; } = "";

        public int seller_id { get; set; }

        public float unit_price { get; set; }

        public DateTime shipping_limit_date { get; set; }

        public float freight_value { get; set; }

        // not present in olist
        public int quantity { get; set; }

        // without freight value
        public float total_items { get; set; }

        // without freight value
        public float total_amount { get; set; }

        // can be derived from total_items - total_amount
        // incentive of item is not of concern to the order
        // the seller must compute 
        // public float total_incentive { get; set; }

        [ForeignKey("customer_id, order_id")]
        public virtual OrderModel order { get; set; }

        public OrderItemModel() { }

    }
}

