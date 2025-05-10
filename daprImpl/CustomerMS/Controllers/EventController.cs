using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.CustomerLibrary.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace OnlineMarket.DaprImpl.CustomerMS.Controllers
{
    [ApiController]
    public class EventController : ControllerBase
    {
        private const string PUBSUB_NAME = "pubsub";

        private readonly ICustomerService customerService;
        private readonly ILogger<CustomerController> logger;

        public EventController(ICustomerService customerService, ILogger<CustomerController> logger)
        {
            this.customerService = customerService;
            this.logger = logger;
        }

        [HttpPost("ProcessFailedReservation")]
        [Topic(PUBSUB_NAME, nameof(ReserveStockFailed))]
        public ActionResult ProcessFailedReservation([FromBody] ReserveStockFailed reserveStockFailed)
        {
            this.logger.LogInformation("[ProcessFailedReservation] received for customer {0}", reserveStockFailed.customerCheckout.CustomerId);
            this.logger.LogInformation("[ProcessFailedReservation] completed for customer {0}.", reserveStockFailed.customerCheckout.CustomerId);
            return Ok();
        }

        [HttpPost("ProcessPaymentConfirmed")]
        [Topic(PUBSUB_NAME, nameof(PaymentConfirmed))]
        public ActionResult ProcessPaymentConfirmed([FromBody] PaymentConfirmed paymentConfirmed)
        {
            this.logger.LogInformation("[ProcessPaymentConfirmed] received for customer {0}", paymentConfirmed.customer.CustomerId);
            this.customerService.ProcessPaymentConfirmed(paymentConfirmed);
            this.logger.LogInformation("[ProcessPaymentConfirmed] completed for customer {0}.", paymentConfirmed.customer.CustomerId);
            return Ok();
        }

        [HttpPost("ProcessPaymentFailed")]
        [Topic(PUBSUB_NAME, nameof(PaymentFailed))]
        public ActionResult ProcessPaymentFailed([FromBody] PaymentFailed paymentFailed)
        {
            this.logger.LogInformation("[ProcessPaymentConfirmed] received for customer {0}", paymentFailed.customer.CustomerId);
            this.customerService.ProcessPaymentFailed(paymentFailed);
            this.logger.LogInformation("[ProcessPaymentConfirmed] completed for customer {0}.", paymentFailed.customer.CustomerId);
            return Ok();
        }

        [HttpPost("ProcessDeliveryNotification")]
        [Topic(PUBSUB_NAME, nameof(DeliveryNotification))]
        public ActionResult ProcessDeliveryNotification([FromBody] DeliveryNotification deliveryNotification)
        {
            this.logger.LogInformation("[ProcessDeliveryNotification] received for customer {0}", deliveryNotification.customerId);
            this.customerService.ProcessDeliveryNotification(deliveryNotification);
            this.logger.LogInformation("[ProcessDeliveryNotification] completed for customer {0}.", deliveryNotification.customerId);
            return Ok();
        }

    }
}

