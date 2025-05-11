using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.SellerLibrary.Services;

namespace OnlineMarket.DaprImpl.SellerMS.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly ILogger<EventController> logger;
    private readonly ISellerService sellerService;

    public EventController(ISellerService sellerService, ILogger<EventController> logger)
    {
        this.sellerService = sellerService;
        this.logger = logger;
    }

    [HttpPost("ProcessNewInvoice")]
    [Topic(PUBSUB_NAME, nameof(InvoiceIssued))]
    public ActionResult ProcessNewInvoice([FromBody] InvoiceIssued invoiceIssued)
    {
        try{
            this.sellerService.ProcessInvoiceIssued(invoiceIssued);
            return Ok();
        } catch(Exception e)
        {
            this.logger.LogCritical(e.ToString());
            this.logger.LogWarning($"Invoice items: {invoiceIssued}");
            return BadRequest(e.ToString());
        }
    }

    /*
    [HttpPost("ProcessPaymentConfirmed")]
    [Topic(PUBSUB_NAME, nameof(PaymentConfirmed))]
    public ActionResult ProcessPaymentConfirmed([FromBody] PaymentConfirmed paymentConfirmed)
    {
        this.logger.LogInformation("[PaymentConfirmed] received for order ID {0}.", paymentConfirmed.orderId);
        this.sellerService.ProcessPaymentConfirmed(paymentConfirmed);
        return Ok();
    }
    */

    [HttpPost("ProcessPaymentFailed")]
    [Topic(PUBSUB_NAME, nameof(PaymentFailed))]
    public ActionResult ProcessPaymentFailed([FromBody] PaymentFailed paymentFailed)
    {
        this.sellerService.ProcessPaymentFailed(paymentFailed);
        return Ok();
    }

    [HttpPost("ProcessShipmentNotification")]
    [Topic(PUBSUB_NAME, nameof(ShipmentNotification))]
    public ActionResult ProcessShipmentNotification([FromBody] ShipmentNotification shipmentNotification)
    {
        try {
            this.sellerService.ProcessShipmentNotification(shipmentNotification);
        } catch(Exception e)
        {
            this.logger.LogCritical(e.Message);
            // concurrency issues are raised if two entities with the same key are tracked by ef core
            // no way to tell dapr to synchronize both events 
        }
        return Ok();
    }

    [HttpPost("ProcessDeliveryNotification")]
    [Topic(PUBSUB_NAME, nameof(DeliveryNotification))]
    public ActionResult ProcessDeliveryNotification([FromBody] DeliveryNotification deliveryNotification)
    {
        this.sellerService.ProcessDeliveryNotification(deliveryNotification);
        return Ok();
    }

}
