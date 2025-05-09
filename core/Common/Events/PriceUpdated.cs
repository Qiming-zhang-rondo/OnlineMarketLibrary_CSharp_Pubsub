namespace OnlineMarket.Core.Common.Events;

public class PriceUpdated
{ 
    public int seller_id { get; set; }
    public int product_id { get; set; }
    public float price { get; set; }
    public string version { get; set; }
    public string instanceId { get; set; }

    public PriceUpdated(){ }

    public PriceUpdated(int seller_id, int product_id, float price, string version, string instanceId)
    {
        this.seller_id = seller_id;
        this.product_id = product_id;
        this.price = price;
        this.version = version;
        this.instanceId = instanceId;
    }

    public override string ToString()
    {
        return $"seller_id:{seller_id}, product_id:{product_id}, version:{version}, price:{price}, instanceId:{instanceId}";
    }
}