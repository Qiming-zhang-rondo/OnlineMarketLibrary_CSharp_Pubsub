using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.PaymentLibrary.Infra;
using OnlineMarket.Core.PaymentLibrary.Models;
using OnlineMarket.Core.PaymentLibrary.Repositories;
using OnlineMarket.Common.Messaging;

namespace OnlineMarket.Core.PaymentLibrary.Services
{
    public class PaymentServiceCore : IPaymentService
    {
        private readonly IPaymentRepository paymentRepository;
        private readonly IExternalProvider externalProvider;
        private readonly IEventPublisher eventPublisher;
        private readonly PaymentConfig config;

        public PaymentServiceCore(
            IPaymentRepository paymentRepository,
            IExternalProvider externalProvider,
            IEventPublisher eventPublisher,
            PaymentConfig config)
        {
            this.paymentRepository = paymentRepository;
            this.externalProvider = externalProvider;
            this.eventPublisher = eventPublisher;
            this.config = config;
        }

        private static readonly string StreamId =
            new StringBuilder(nameof(TransactionMark))
                .Append('_')
                .Append(TransactionType.CUSTOMER_SESSION.ToString())
                .ToString();

        public async Task ProcessPayment(InvoiceIssued invoiceIssued)
        {
            var cardExpParsed = DateTime.ParseExact(invoiceIssued.customer.CardExpiration, "MMyy", CultureInfo.InvariantCulture);
            PaymentStatus status;

            if (config.PaymentProvider)
            {
                var options = new PaymentIntentCreateOptions()
                {
                    Amount = invoiceIssued.totalInvoice,
                    Customer = invoiceIssued.customer.CustomerId.ToString(),
                    IdempotencyKey = invoiceIssued.invoiceNumber,
                    cardOptions = new()
                    {
                        Number = invoiceIssued.customer.CardNumber,
                        Cvc = invoiceIssued.customer.CardSecurityNumber,
                        ExpMonth = cardExpParsed.Month.ToString(),
                        ExpYear = cardExpParsed.Year.ToString()
                    }
                };

                PaymentIntent? intent = externalProvider.Create(options);

                if (intent is null)
                {
                    throw new Exception("[ProcessPayment] It was not possible to retrieve payment intent from external provider");
                }

                status = intent.status.Equals("succeeded")
                    ? PaymentStatus.succeeded
                    : PaymentStatus.requires_payment_method;
            }
            else
            {
                status = PaymentStatus.succeeded;
            }

            var now = DateTime.UtcNow;
            using (var txCtx = this.paymentRepository.BeginTransaction())
            {
                int seq = 1;
                var cc = invoiceIssued.customer.PaymentType.Equals(PaymentType.CREDIT_CARD.ToString());

                if (cc || invoiceIssued.customer.PaymentType.Equals(PaymentType.DEBIT_CARD.ToString()))
                {
                    var cardPaymentLine = new OrderPaymentModel()
                    {
                        customer_id = invoiceIssued.customer.CustomerId,
                        order_id = invoiceIssued.orderId,
                        sequential = seq,
                        type = cc ? PaymentType.CREDIT_CARD : PaymentType.DEBIT_CARD,
                        installments = invoiceIssued.customer.Installments,
                        value = invoiceIssued.totalInvoice,
                        status = status,
                        created_at = now
                    };

                    var entity = this.paymentRepository.Insert(cardPaymentLine);
                    this.paymentRepository.FlushUpdates();

                    OrderPaymentCardModel card = new()
                    {
                        customer_id = invoiceIssued.customer.CustomerId,
                        order_id = invoiceIssued.orderId,
                        sequential = seq,
                        card_number = invoiceIssued.customer.CardNumber,
                        card_holder_name = invoiceIssued.customer.CardHolderName,
                        card_expiration = cardExpParsed,
                        card_brand = invoiceIssued.customer.CardBrand,
                        orderPayment = entity
                    };

                    this.paymentRepository.Insert(card);
                    seq++;
                }

                List<OrderPaymentModel> paymentLines = new();
                if (invoiceIssued.customer.PaymentType.Equals(PaymentType.BOLETO.ToString()))
                {
                    paymentLines.Add(new OrderPaymentModel()
                    {
                        customer_id = invoiceIssued.customer.CustomerId,
                        order_id = invoiceIssued.orderId,
                        sequential = seq,
                        type = PaymentType.BOLETO,
                        installments = 1,
                        value = invoiceIssued.totalInvoice,
                        created_at = now,
                        status = status
                    });
                    seq++;
                }

                if (status == PaymentStatus.succeeded)
                {
                    foreach (var item in invoiceIssued.items)
                    {
                        if (item.total_incentive > 0)
                        {
                            paymentLines.Add(new OrderPaymentModel()
                            {
                                customer_id = invoiceIssued.customer.CustomerId,
                                order_id = invoiceIssued.orderId,
                                sequential = seq,
                                type = PaymentType.VOUCHER,
                                installments = 1,
                                value = item.total_incentive,
                                created_at = now,
                                status = status
                            });
                            seq++;
                        }
                    }
                }

                if (paymentLines.Count > 0)
                {
                    this.paymentRepository.InsertAll(paymentLines);
                }

                this.paymentRepository.FlushUpdates();
                txCtx.Commit();
            }

            if (this.config.Streaming)
            {
                if (status == PaymentStatus.succeeded)
                {
                    var paymentRes = new PaymentConfirmed(invoiceIssued.customer, invoiceIssued.orderId,
                        invoiceIssued.totalInvoice, invoiceIssued.items, now, invoiceIssued.instanceId);

                    await this.eventPublisher.PublishEventAsync(nameof(PaymentConfirmed), paymentRes);
                }
                else
                {
                    var res = new PaymentFailed(status.ToString(), invoiceIssued.customer, invoiceIssued.orderId,
                            invoiceIssued.items, invoiceIssued.totalInvoice, invoiceIssued.instanceId);

                    await this.eventPublisher.PublishEventAsync(nameof(PaymentFailed), res);

                    await this.eventPublisher.PublishEventAsync(StreamId, new TransactionMark(invoiceIssued.instanceId,
                        TransactionType.CUSTOMER_SESSION, invoiceIssued.customer.CustomerId,
                        MarkStatus.NOT_ACCEPTED, "payment"));
                }
            }
        }

        public async Task ProcessPoisonPayment(InvoiceIssued paymentRequest)
        {
            await this.eventPublisher.PublishEventAsync(StreamId,
                new TransactionMark(paymentRequest.instanceId,
                    TransactionType.CUSTOMER_SESSION,
                    paymentRequest.customer.CustomerId,
                    MarkStatus.ABORT,
                    "payment"));
        }

        public void Cleanup()
        {
            this.paymentRepository.Cleanup();
        }
    }
}