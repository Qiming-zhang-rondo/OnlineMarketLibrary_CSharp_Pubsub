using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.ShipmentLibrary.Infra;
using OnlineMarket.Core.ShipmentLibrary.Models;
using OnlineMarket.Core.ShipmentLibrary.Repositories;

namespace OnlineMarket.DaprImpl.ShipmentMS.Repositories;

public class InMemoryPackageRepository : IPackageRepository
{
    private readonly ConcurrentDictionary<(int customerId, int orderId, int packageId),PackageModel> packages;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryPackageRepository(IOptions<ShipmentConfig> config)
    { 
        this.packages = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "package");
	}

    public void Insert(PackageModel item)
    {
        this.packages.TryAdd((item.customer_id,item.order_id, item.package_id), item);
        this.logging.Append(item);
    }

    public void InsertAll(List<PackageModel> values)
    {
        foreach(var pkg in values)
        {
            this.Insert(pkg);
        }
    }

    public void Update(PackageModel newValue)
    {
        this.packages[(newValue.customer_id,newValue.order_id, newValue.package_id)] = newValue;
        this.logging.Append(newValue);
    }

    public void Delete((int, int, int) id)
    {
        this.packages.Remove(id, out var item);
        if(item is not null)
            this.logging.Append(item);
    }

    public PackageModel? GetById((int, int, int) id)
    {
        return this.packages[id];
    }

    public IDictionary<int, string[]> GetOldestOpenShipmentPerSeller()
    {
        return this.packages.Values
                .Where(x => x.status.Equals(PackageStatus.shipped))
                .GroupBy(x => x.seller_id)
                .Select(g => new { key = g.Key, Sort = g.Min(x => x.GetOrderIdAsString()) }).Take(10)
                .ToDictionary(g => g.key, g => {
                    if(g.Sort is null) return Array.Empty<string>();
                    return g.Sort.Split("|");
                });
}

    public IEnumerable<PackageModel> GetShippedPackagesByOrderAndSeller(int customerId, int orderId, int sellerId)
    {
        return this.packages.Values.Where(p => p.customer_id == customerId && p.order_id == orderId && p.status == PackageStatus.shipped && p.seller_id == sellerId);
    }

    public int GetTotalDeliveredPackagesForOrder(int customerId, int orderId)
    {
        return this.packages.Values.Where(p => p.customer_id == customerId && p.order_id == orderId && p.status == PackageStatus.delivered).Count();
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
        this.packages.Clear();
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
