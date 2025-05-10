using System.Net;
using OnlineMarket.Core.Common.Events;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.PaymentLibrary.Models;
using OnlineMarket.Core.PaymentLibrary.Repositories;
using OnlineMarket.Core.PaymentLibrary.Services;

namespace OnlineMarket.DaprImpl.PaymentMS.Controllers;

[ApiController]
public class PaymentController : ControllerBase
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly IPaymentService paymentService;
    private readonly IPaymentRepository paymentRepository;
    private readonly ILogger<PaymentController> logger;
    
    public PaymentController(IPaymentService paymentService, IPaymentRepository paymentRepository, ILogger<PaymentController> logger)
    {
        this.paymentService = paymentService;
        this.paymentRepository = paymentRepository;
        this.logger = logger;
    }

    [HttpPost("ProcessPayment")]
    [Topic(PUBSUB_NAME, nameof(InvoiceIssued))]
    public async Task<ActionResult> ProcessPayment([FromBody] InvoiceIssued invoiceIssued)
    {
        try
        {
            await this.paymentService.ProcessPayment(invoiceIssued);
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e.ToString());
            await this.paymentService.ProcessPoisonPayment(invoiceIssued);
        }
        return Ok();
    }

    [HttpGet]
    [Route("{customerId}/{orderId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult<IEnumerable<OrderPaymentModel>> GetPaymentByOrderId(int customerId, int orderId)
    {
        var res = this.paymentRepository.GetByOrderId(customerId, orderId);
        return res is not null ? Ok( res ) : NotFound();
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        this.logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.paymentService.Cleanup();
        return Ok();
    }

}

