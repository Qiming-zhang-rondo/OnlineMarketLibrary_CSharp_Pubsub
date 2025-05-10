using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.PaymentProviderLibrary.Service;
using System.Net;

namespace PaymentProvider.Controllers;

[ApiController]
public class PaymentProviderController : ControllerBase
{
    private readonly IPaymentProvider service;
    private readonly ILogger<PaymentProviderController> logger;

    public PaymentProviderController(IPaymentProvider service, ILogger<PaymentProviderController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpPost]
    [Route("/esp")]
    [ProducesResponseType(typeof(PaymentIntent),(int)HttpStatusCode.OK)]
    public ActionResult<PaymentIntent> ProcessPaymentIntent([FromBody] PaymentIntentCreateOptions options)
    {
        return Ok(this.service.ProcessPaymentIntent(options));
    }
}

