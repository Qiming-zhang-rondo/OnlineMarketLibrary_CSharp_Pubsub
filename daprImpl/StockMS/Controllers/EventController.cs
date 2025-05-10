using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Events;
using StockMS.Services;

namespace StockMS.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly ILogger<EventController> logger;
    private readonly IStockService stockService;

    public EventController(IStockService stockService, ILogger<EventController> logger)
    {
        this.stockService = stockService;
        this.logger = logger;
    }

    [HttpPost("ProcessProductUpdate")]
    [Topic(PUBSUB_NAME, nameof(ProductUpdated))]
    public async Task<ActionResult> ProcessProductUpdate([FromBody] ProductUpdated productUpdated)
    {
        try
        {
            await this.stockService.ProcessProductUpdate(productUpdated);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
            await this.stockService.ProcessPoisonProductUpdate(productUpdated);
        }
        return Ok();
    }

    [HttpPost("ProcessReserveStock")]
    [Topic(PUBSUB_NAME, nameof(ReserveStock))]
    public async Task<ActionResult> ProcessReserveStock([FromBody] ReserveStock reserveStock)
    {
        try
        {
            await this.stockService.ReserveStockAsync(reserveStock);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
            await this.stockService.ProcessPoisonReserveStock(reserveStock);
        }
        return Ok();
    }

    [HttpPost("ProcessPaymentConfirmed")]
    [Topic(PUBSUB_NAME, nameof(PaymentConfirmed))]
    public ActionResult ProcessPaymentConfirmed([FromBody] PaymentConfirmed paymentConfirmed)
    {
        try
        {
            this.stockService.ConfirmReservation(paymentConfirmed);
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
         try
         {
            this.stockService.CancelReservation(paymentFailed);
         }
         catch (Exception e)
         {
            this.logger.LogCritical(e.ToString());
         }
        return Ok();
    }

}