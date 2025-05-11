using OnlineMarket.Core.ShipmentLibrary.Models;

namespace OnlineMarket.Core.ShipmentLibrary.Repositories;

public interface IShipmentRepository : IRepository<(int,int), ShipmentModel>
{
    void Cleanup();
}
