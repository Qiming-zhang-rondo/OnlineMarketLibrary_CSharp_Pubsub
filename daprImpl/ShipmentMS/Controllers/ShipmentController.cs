using System.Net;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.ShipmentLibrary.Models;
using OnlineMarket.Core.ShipmentLibrary.Repositories;
using OnlineMarket.Core.ShipmentLibrary.Services;

namespace ShipmentMS.Controllers;

[ApiController]
public class ShipmentController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly IShipmentService shipmentService;
    private readonly IShipmentRepository shipmentRepository;
    private readonly ILogger<ShipmentController> logger;

    public ShipmentController(IShipmentService shipmentService, IShipmentRepository shipmentRepository, ILogger<ShipmentController> logger)
    {
        this.shipmentService = shipmentService;
        this.shipmentRepository = shipmentRepository;
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

    [HttpPatch]
    [Route("{instanceId}")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public async Task<ActionResult> UpdateShipment(string instanceId)
    {
        await this.shipmentService.UpdateShipment(instanceId);
        return Accepted();
    }

    [HttpGet]
    [Route("{customerId}/{orderId}")]
    [ProducesResponseType(typeof(Shipment), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult<Shipment> GetShipment(int customerId, int orderId)
    {
        ShipmentModel? shipment = this.shipmentRepository.GetById((customerId, orderId));
        if (shipment is not null)
            return Ok(new Shipment()
            {
                order_id = shipment.order_id,
                customer_id = shipment.customer_id,
                package_count = shipment.package_count,
                total_freight_value = shipment.total_freight_value,
                request_date = shipment.request_date,
                status = shipment.status,
                first_name = shipment.first_name,
                last_name = shipment.last_name,
                street = shipment.street,
                complement = shipment.complement,
                zip_code = shipment.zip_code,
                city = shipment.zip_code,
                state = shipment.state
            });
        return NotFound();
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        this.logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.shipmentService.Cleanup();
        return Ok();
    }

}
