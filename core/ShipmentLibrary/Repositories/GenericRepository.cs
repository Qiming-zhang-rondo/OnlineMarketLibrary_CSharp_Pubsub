// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
//
// namespace OnlineMarket.Core.ShipmentLibrary.Repositories;
//
// /**
//  * Based on https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application
//  */
// public class GenericRepository<PK, TEntity> : IRepository<PK, TEntity> where TEntity : class
// {
//     internal ShipmentDbContext context;
//     internal DbSet<TEntity> dbSet;
//
//     public GenericRepository(ShipmentDbContext context)
//     {
//         this.context = context;
//         this.dbSet = context.Set<TEntity>();
//     }
//
//     public void Save()
//     {
//         this.context.SaveChanges();
//     }
//
//     public virtual TEntity? GetById(PK id)
//     {
//         return this.dbSet.Find(id);
//     }
//
//     public virtual void Insert(TEntity entity)
//     {
//         this.dbSet.Add(entity);
//     }
//
//     public void InsertAll(List<TEntity> values)
//     {
//         this.dbSet.AddRange(values);
//     }
//
//     public virtual void Delete(PK id)
//     {
//         var entityToDelete = this.GetById(id);
//         if(entityToDelete is not null) Delete(entityToDelete);
//     }
//
//     public virtual void Delete(TEntity entityToDelete)
//     {
//         if (context.Entry(entityToDelete).State == EntityState.Detached)
//         {
//             dbSet.Attach(entityToDelete);
//         }
//         dbSet.Remove(entityToDelete);
//     }
//
//     public virtual void Update(TEntity entityToUpdate)
//     {
//         dbSet.Attach(entityToUpdate);
//         context.Entry(entityToUpdate).State = EntityState.Modified;
//     }
//
//     private bool disposed = false;
//
//     protected virtual void Dispose(bool disposing)
//     {
//         if (!this.disposed)
//         {
//             if (disposing)
//             {
//                 context.Dispose();
//             }
//         }
//         this.disposed = true;
//     }
//
//     public void Dispose()
//     {
//         Dispose(true);
//         GC.SuppressFinalize(this);
//     }
//
//     public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel)
//     {
//         return this.context.Database.BeginTransaction(isolationLevel);
//     }
// }
