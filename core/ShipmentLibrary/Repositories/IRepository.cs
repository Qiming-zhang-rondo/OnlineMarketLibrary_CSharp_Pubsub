using Microsoft.EntityFrameworkCore.Storage;
using System.Data;


namespace OnlineMarket.Core.ShipmentLibrary.Repositories;

public interface IRepository<PK,T> : IDisposable
{

    T? GetById(PK id);

    void Insert(T value);

    void InsertAll(List<T> values);

    void Delete(PK id);

    void Update(T newValue);

    void Save();

    // API for ShipmentService
    IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot);

}
