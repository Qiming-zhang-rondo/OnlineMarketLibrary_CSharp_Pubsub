using OnlineMarket.Core.ShipmentLibrary.Models;

namespace OnlineMarket.Core.ShipmentLibrary.Repositories;

public interface IPackageRepository : IRepository<(int,int,int), PackageModel>
{
    IDictionary<int, string[]> GetOldestOpenShipmentPerSeller();

    IEnumerable<PackageModel> GetShippedPackagesByOrderAndSeller(int customerId, int orderId, int sellerId);

    int GetTotalDeliveredPackagesForOrder(int customerId, int orderId);

    void Cleanup();
}