namespace OnlineMarket.Core.Common.Events;

public class ProductUpdated {

    public int seller_id { get; set; }

    public int product_id { get; set; }

    public string name { get; set; }

    public string sku { get; set; }

    public string category { get; set; }

    public string description { get; set; }

    public float price { get; set; }

    public float freight_value { get; set; }

    public string status { get; set; }

    public string version { get; set; }

    public ProductUpdated(){ }

    public ProductUpdated(int seller_id, int product_id, string name, string sku, string category, string description, float price, float freight_value, string status, string version) {
        this.seller_id = seller_id;
        this.product_id = product_id;
        this.name = name;
        this.sku = sku;
        this.category = category;
        this.description = description;
        this.price = price;
        this.freight_value = freight_value;
        this.status = status;
        this.version = version;
    }

    public override string ToString()
    {
        return $" seller_id: {seller_id}, product_id: {product_id}, version: {version}";
    }

}