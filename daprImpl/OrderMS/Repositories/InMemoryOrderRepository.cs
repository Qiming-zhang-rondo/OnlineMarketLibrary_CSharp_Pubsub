using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnlineMarket.Core.Common.Infra;
using OnlineMarket.Core.OrderLibrary.Infra;
using OnlineMarket.Core.OrderLibrary.Models;
using OnlineMarket.Core.OrderLibrary.Repositories;

namespace OnlineMarket.Core.OrderMS.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<(int customerId, int orderId),OrderModel> orders;
    private readonly ConcurrentDictionary<int,CustomerOrderModel> customerOrders;

    private readonly ConcurrentDictionary<(int customerId, int orderId),List<OrderItemModel>> orderItems;
    private readonly ConcurrentDictionary<(int customerId, int orderId),List<OrderHistoryModel>> orderHistory;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryOrderRepository(IOptions<OrderConfig> config)
	{
        this.orders = new();
        this.customerOrders = new();
        this.orderItems = new();
        this.orderHistory = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "order");
	}

    public IEnumerable<OrderModel> GetAll()
    {
        return this.orders.Values;
    }

    public IEnumerable<OrderModel> GetByCustomerId(int customerId)
    {
        return this.orders.Values.Where(o=> o.customer_id == customerId);
    }

    public OrderModel? GetOrder(int customerId, int orderId)
    {
        return this.orders[(customerId, orderId)];
    }

    public OrderModel InsertOrder(OrderModel order)
    {
        this.orders.TryAdd((order.customer_id,order.order_id), order);
        this.logging.Append(order);
        return order;
    }

    public OrderModel UpdateOrder(OrderModel order)
    {
        this.orders[(order.customer_id,order.order_id)] = order;
        this.logging.Append(order);
        return order;
    }

    public CustomerOrderModel? GetCustomerOrderByCustomerId(int customerId)
    {
        if(this.customerOrders.ContainsKey(customerId))
            return this.customerOrders[customerId];
        return null;
    }

    public CustomerOrderModel InsertCustomerOrder(CustomerOrderModel customerOrder)
    {
        this.customerOrders.TryAdd(customerOrder.customer_id, customerOrder);
        this.logging.Append(customerOrder);
        return customerOrder;
    }

    public CustomerOrderModel UpdateCustomerOrder(CustomerOrderModel customerOrder)
    {
        this.customerOrders[customerOrder.customer_id] = customerOrder;
        this.logging.Append(customerOrder);
        return customerOrder;
    }

    public OrderItemModel InsertOrderItem(OrderItemModel orderItem)
    {
        if(!this.orderItems.ContainsKey((orderItem.customer_id, orderItem.order_id))){
            this.orderItems[(orderItem.customer_id, orderItem.order_id)] = new();
        }
        this.orderItems[(orderItem.customer_id, orderItem.order_id)].Add(orderItem);
        this.logging.Append(orderItem);
        return orderItem;
    }

    public OrderHistoryModel InsertOrderHistory(OrderHistoryModel orderHistory)
    {
        if(!this.orderHistory.ContainsKey((orderHistory.customer_id, orderHistory.order_id))){
            this.orderHistory[(orderHistory.customer_id, orderHistory.order_id)] = new();
        }
        this.orderHistory[(orderHistory.customer_id, orderHistory.order_id)].Add(orderHistory);
        this.logging.Append(orderHistory);
        return orderHistory;
    }

    public void FlushUpdates()
    {
        // do nothing
    }

    public void Cleanup()
    {
        this.orderHistory.Clear();
        this.orderItems.Clear();
        this.orders.Clear();
        this.customerOrders.Clear();
        this.logging.Clear();
    }

    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
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


