using System.ComponentModel.DataAnnotations.Schema;
using OnlineMarket.Core.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace OnlineMarket.Core.CartLibrary.Models
{
    [Table("carts", Schema = "cart")]
    [PrimaryKey(nameof(customer_id))]
    public class CartModel
	{
        public int customer_id { get; set; }

        public CartStatus status { get; set; } = CartStatus.OPEN;

        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }

        public CartModel() {
            this.created_at = DateTime.UtcNow;
            this.updated_at = this.created_at;
        }
    }
}

