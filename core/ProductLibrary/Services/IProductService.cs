using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.ProductLibrary.Services
{
	public interface IProductService
	{
        void ProcessCreateProduct(Product product);

        Task ProcessProductUpdate(Product product);
        Task ProcessPoisonProductUpdate(Product product);

        Task ProcessPriceUpdate(PriceUpdate priceUpdate);
        Task ProcessPoisonPriceUpdate(PriceUpdate product);

        void Cleanup();
        void Reset();
        
    }
}