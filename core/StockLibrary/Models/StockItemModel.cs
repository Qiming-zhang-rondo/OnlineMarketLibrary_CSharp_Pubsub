using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnlineMarket.Core.StockLibrary.Models;

[Table("stock_items", Schema = "stock")]
[PrimaryKey(nameof(seller_id),nameof(product_id))]
public class StockItemModel
{
    // consider partitioning on seller id
    // https://www.postgresql.org/docs/current/ddl-partitioning.html
    // https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing?tabs=dotnet-core-cli#adding-raw-sql
    public int seller_id { get; set; }

    public int product_id { get; set; }

    public int qty_available { get; set; }

    public int qty_reserved { get; set; } = 0;

    public int order_count { get; set; } = 0;

    public int ytd { get; set; } = 0;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public string data { get; set; }

    public string version { get; set; }

    public bool active { get; set; }

    public StockItemModel() { }

    public StockItemModel(int product_id, int seller_id, int qty, DateTime created_at)
    {
        this.product_id = product_id;
        this.seller_id = seller_id;
        this.qty_available = qty;
        this.created_at = created_at;
        this.updated_at = created_at;
        this.active = true;
    }
}