using System;
using System.Threading.Tasks;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.OrderLibrary.Services;

namespace OnlineMarket.DaprImpl.OrderMS.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly IOrderService orderService;

    private readonly ILogger<EventHandler> logger;

    public EventController(IOrderService orderService, ILogger<EventHandler> logger)
    {
        this.orderService = orderService;
        this.logger = logger;
    }

    [HttpPost("ProcessStockConfirmed")]
    [Topic(PUBSUB_NAME, nameof(StockConfirmed))]
    public async Task<ActionResult> ProcessStockConfirmed([FromBody] StockConfirmed stockConfirmed)
    {
        try
        {
            await this.orderService.ProcessStockConfirmed(stockConfirmed);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
            await this.orderService.ProcessPoisonStockConfirmed(stockConfirmed);
        }
        return Ok();
    }

    
    [HttpPost("ProcessPaymentConfirmed")]
    [Topic(PUBSUB_NAME, nameof(PaymentConfirmed))]
    public ActionResult ProcessPaymentConfirmed([FromBody] PaymentConfirmed paymentConfirmed)
    {
         try
         {
            this.orderService.ProcessPaymentConfirmed(paymentConfirmed);
         }
         catch (Exception e)
         {
             this.logger.LogCritical(e.ToString());
         }
        return Ok();
    }

    [HttpPost("ProcessPaymentFailed")]
    [Topic(PUBSUB_NAME, nameof(PaymentFailed))]
    public ActionResult ProcessPaymentFailed([FromBody] PaymentFailed paymentFailed)
    {
        this.orderService.ProcessPaymentFailed(paymentFailed);
        return Ok();
    }

    [HttpPost("ProcessShipmentNotification")]
    [Topic(PUBSUB_NAME, nameof(ShipmentNotification))]
    public ActionResult ProcessShipmentNotification([FromBody] ShipmentNotification shipmentNotification)
    {
        try {
            this.orderService.ProcessShipmentNotification(shipmentNotification);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
        }
        return Ok();
    }
    
}

