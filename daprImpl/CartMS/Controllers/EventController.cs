using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.CartLibrary.Services;
using OnlineMarket.Core.Common.Events;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace OnlineMarket.DaprImpl.CartMS.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly DaprClient daprClient;
    private readonly ICartRepository cartRepository;
    private readonly IProductReplicaRepository productReplicaRepository;

    private readonly ILogger<EventController> logger;
    private readonly ICartService cartService;

    public EventController(DaprClient daprClient,
                            ICartService cartService,
                            ICartRepository cartRepository,
                            IProductReplicaRepository productReplicaRepository,
                            ILogger<EventController> logger)
    {
        this.daprClient = daprClient;
        this.cartRepository = cartRepository;
        this.productReplicaRepository = productReplicaRepository;
        this.logger = logger;
        this.cartService = cartService;
    }

    [HttpPost("ProcessProductUpdate")]
    [Topic(PUBSUB_NAME, nameof(ProductUpdated))]
    public async Task<ActionResult> ProcessProductUpdate([FromBody] ProductUpdated productUpdated)
    {
        try
        {
            this.cartService.ProcessProductUpdated(productUpdated);
        }
        catch (Exception e)
        {
            logger.LogCritical(e.ToString());
            await this.cartService.ProcessPoisonProductUpdated(productUpdated);
        }
        return Ok();
    }

    [HttpPost("ProcessPriceUpdate")]
    [Topic(PUBSUB_NAME, nameof(PriceUpdated))]
    public async Task<ActionResult> ProcessPriceUpdate([FromBody] PriceUpdated priceUpdated)
    {
        try
        {
            await this.cartService.ProcessPriceUpdate(priceUpdated);
        }
        catch (Exception e)
        {
            logger.LogCritical(e.ToString());
            await this.cartService.ProcessPoisonPriceUpdate(priceUpdated);
        }
        return Ok();
    }

}