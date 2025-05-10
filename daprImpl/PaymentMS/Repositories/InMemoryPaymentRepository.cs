using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Infra;
using Microsoft.EntityFrameworkCore.Storage;
using OnlineMarket.Core.PaymentLibrary.Infra;
using OnlineMarket.Core.PaymentLibrary.Models;
using OnlineMarket.Core.PaymentLibrary.Repositories;
using Microsoft.Extensions.Options;

namespace OnlineMarket.DaprImpl.PaymentMS.Repositories;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<(int customerId, int orderId, int packageId),OrderPaymentModel> orderPayments;
    private readonly ConcurrentDictionary<(int customerId, int orderId, int packageId),OrderPaymentCardModel> orderPaymentCards;

    private readonly ILogging logging;

    private static readonly IDbContextTransaction DEFAULT_DB_TX = new NoTransactionScope();

	public InMemoryPaymentRepository(IOptions<PaymentConfig> config)
	{
        this.orderPayments = new();
        this.orderPaymentCards = new();
        this.logging = LoggingHelper.Init(config.Value.Logging, config.Value.LoggingDelay, "payment");
	}

    public OrderPaymentCardModel Insert(OrderPaymentCardModel orderPaymentCard)
    {
        this.orderPaymentCards.TryAdd((orderPaymentCard.customer_id, orderPaymentCard.order_id, orderPaymentCard.sequential), orderPaymentCard);
        this.logging.Append(orderPaymentCard);
        return orderPaymentCard;
    }

    public OrderPaymentModel Insert(OrderPaymentModel orderPayment)
    {
        this.orderPayments.TryAdd((orderPayment.customer_id, orderPayment.order_id, orderPayment.sequential), orderPayment);
        this.logging.Append(orderPayment);
        return orderPayment;
    }

    public IEnumerable<OrderPaymentModel> GetByOrderId(int customerId, int orderId)
    {
        return this.orderPayments.Values.Where(o=>o.customer_id == customerId && o.order_id == orderId);
    }

    public void InsertAll(List<OrderPaymentModel> orderPayments)
    {
        foreach(var orderPayment in orderPayments)
        {
            this.orderPayments.TryAdd((orderPayment.customer_id, orderPayment.order_id, orderPayment.sequential), orderPayment);
        }
    }

    public IDbContextTransaction BeginTransaction()
    {
        return DEFAULT_DB_TX;
    }

    public void Cleanup()
    {
        this.orderPaymentCards.Clear();
        this.orderPayments.Clear();
        this.logging.Clear();
    }

    public void FlushUpdates()
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

