using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.OrderLibrary.Models
{
    // https://learn.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
    [Table("order_history", Schema = "order")]
    [PrimaryKey(nameof(id))]
    public class OrderHistoryModel
    {
        public int id { get; set; }

        public int customer_id { get; set; }

        public int order_id { get; set; }

        public DateTime created_at { get; set; }

        public OrderStatus status { get; set; }
        
        [ForeignKey("customer_id, order_id")]
        public virtual OrderModel order { get; set; }

        public OrderHistoryModel() { }

    }
}

