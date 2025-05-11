using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.ShipmentLibrary.Infra;
using OnlineMarket.Core.ShipmentLibrary.Models;
using OnlineMarket.Core.ShipmentLibrary.Repositories;

namespace OnlineMarket.DaprImpl.ShipmentMS.Repositories;

public class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly ConcurrentDictionary<(int customerId, int orderId),ShipmentModel> shipments;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryShipmentRepository(IOptions<ShipmentConfig> config)
	{
        this.shipments = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "shipment");
	}

    public void Insert(ShipmentModel item)
    {
        this.shipments.TryAdd((item.customer_id,item.order_id), item);
        this.logging.Append(item);
    }

    public void InsertAll(List<ShipmentModel> shipments)
    {
        foreach(var ship in shipments)
        {
            this.Insert(ship);
        }
    }

    public void Update(ShipmentModel newValue)
    {
        this.shipments[(newValue.customer_id,newValue.order_id)] = newValue;
        this.logging.Append(newValue);
    }

    public void Delete((int,int) id)
    {
        this.shipments.Remove(id, out var item);
        if(item is not null)
            this.logging.Append(item);
    }

    public ShipmentModel? GetById((int,int) id)
    {
        return this.shipments[id];
    }

    public void Save()
    {
        // do nothing
    }

    public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        return DEFAULT_DB_TX;
    }

    public void Cleanup()
    {
        this.shipments.Clear();
        this.logging.Clear();
    }

    public void Dispose()
    {
        // do nothing
    }

    public class NoTransactionScope : IDbContextTransaction
    {
        public Guid TransactionId => throw new NotImplementedException();

        public void Commit()
        {
            // do nothing
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // do nothing
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
