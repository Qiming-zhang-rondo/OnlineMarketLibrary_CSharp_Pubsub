using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.ProductLibrary.Models;

namespace OnlineMarket.Core.ProductLibrary.Infra
{
	public sealed class Utils
	{
        public static ProductModel AsProductModel(Product product)
        {
            return new()
            {
                product_id = product.product_id,
                seller_id = product.seller_id,
                name = product.name,
                sku = product.sku,
                category = product.category,
                description = product.description,
                price = product.price,
                freight_value = product.freight_value,
                status = product.status,
                version = product.version
            };

        }

        public static Product AsProduct(ProductModel product)
        {
            return new()
            {
                product_id = product.product_id,
                seller_id = product.seller_id,
                name = product.name,
                sku = product.sku,
                category = product.category,
                description = product.description,
                price = product.price,
                freight_value = product.freight_value,
                status = product.status,
                version = product.version
            };

        }

    }
}

