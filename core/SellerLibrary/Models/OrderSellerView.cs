namespace OnlineMarket.Core.SellerLibrary.Models;

/**
 * View
 * Overview of orders in progress
 */
public class OrderSellerView
{
    public int seller_id { get; set; }

    // information below from a seller's perspective

    // order
    public int count_orders { get; set; } = 0;
    public int count_items { get; set; } = 0;
    public float total_amount { get; set; } = 0;
    public float total_freight { get; set; } = 0;

    public float total_incentive { get; set; } = 0;

    public float total_invoice { get; set; } = 0;
    public float total_items { get; set; } = 0;

    public OrderSellerView()
    {
    }
}
