using System.Net;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.ShipmentLibrary.Services;

namespace OnlineMarket.DaprImpl.ShipmentMS.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly IShipmentService shipmentService;
    private readonly ILogger<EventController> logger;

    public EventController(IShipmentService shipmentService, ILogger<EventController> logger)
    {
        this.shipmentService = shipmentService;
        this.logger = logger;
    }

    [HttpPost("ProcessShipment")]
    [Topic(PUBSUB_NAME, nameof(PaymentConfirmed))]
    public async Task<ActionResult> ProcessShipment([FromBody] PaymentConfirmed paymentConfirmed)
    {
        try
        {
            await this.shipmentService.ProcessShipment(paymentConfirmed);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
            await this.shipmentService.ProcessPoisonShipment(paymentConfirmed);
        }
        return Ok();
    }
}