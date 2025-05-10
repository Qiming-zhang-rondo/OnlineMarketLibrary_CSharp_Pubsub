using Microsoft.EntityFrameworkCore.Storage;
using OnlineMarket.Core.OrderLibrary.Models;

namespace OnlineMarket.Core.OrderLibrary.Repositories;

public interface IOrderRepository
{
    IEnumerable<OrderModel> GetAll();
    IEnumerable<OrderModel> GetByCustomerId(int customerId);
    OrderModel? GetOrder(int customerId, int orderId);
    OrderModel InsertOrder(OrderModel order);
    OrderModel UpdateOrder(OrderModel order);

    // APIs for OrderService
    IDbContextTransaction BeginTransaction();
    CustomerOrderModel? GetCustomerOrderByCustomerId(int customerId);
    CustomerOrderModel InsertCustomerOrder(CustomerOrderModel customerOrder);
    CustomerOrderModel UpdateCustomerOrder(CustomerOrderModel customerOrder);
    OrderItemModel InsertOrderItem(OrderItemModel orderItem);
    OrderHistoryModel InsertOrderHistory(OrderHistoryModel orderHistory);
    void FlushUpdates();
    void Cleanup();

}


