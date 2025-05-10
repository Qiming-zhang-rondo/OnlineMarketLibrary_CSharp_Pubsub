using System.Net;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.CustomerLibrary.Models;
using OnlineMarket.Core.CustomerLibrary.Repositories;
using OnlineMarket.Core.CustomerLibrary.Services;
using Microsoft.AspNetCore.Mvc;

namespace OnlineMarket.DaprImpl.CustomerMS.Controllers;
[ApiController]
public class CustomerController : ControllerBase
{

    private const string PUBSUB_NAME = "pubsub";

    private readonly ICustomerService customerService;
    private readonly ICustomerRepository customerRepository;
    private readonly ILogger<CustomerController> logger;

    public CustomerController(ICustomerService customerService, ICustomerRepository customerRepository, ILogger<CustomerController> logger)
    {
        this.customerService = customerService;
        this.customerRepository = customerRepository;
        this.logger = logger;
    }

    [HttpPost("/")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public ActionResult AddCustomer([FromBody] Customer customer)
    {
        this.logger.LogInformation("[AddCustomer] received for id {0}", customer.id);
        this.customerRepository.Insert(new CustomerModel()
        {
            id = customer.id,
            first_name = customer.first_name,
            last_name = customer.last_name,
            address = customer.address,
            complement = customer.complement,
            birth_date = customer.birth_date,
            zip_code = customer.zip_code,
            city = customer.city,
            state = customer.state,
            card_number = customer.card_number,
            card_security_number = customer.card_security_number,
            card_expiration = customer.card_expiration,
            card_holder_name = customer.card_holder_name,
            card_type = customer.card_type,
            success_payment_count = customer.success_payment_count,
            failed_payment_count = customer.failed_payment_count,
            delivery_count = customer.delivery_count,
            data = customer.first_name,
        });
        this.logger.LogInformation("[AddCustomer] completed for id {0}.", customer.id);
        return StatusCode((int)HttpStatusCode.Created);
    }

    [HttpGet("{customerId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult<Customer> GetCustomerById(int customerId)
    {
        this.logger.LogInformation("[GetCustomerById] received for customer ID {0}", customerId);
        CustomerModel? customer = this.customerRepository.GetById(customerId);

        this.logger.LogInformation("[GetCustomerById] completed for customer ID {0}.", customerId);

        if (customer is not null) return Ok(new Customer()
        {
            id = customer.id,
            first_name = customer.first_name,
            last_name = customer.last_name,
            address = customer.address,
            complement = customer.complement,
            birth_date = customer.birth_date,
            zip_code = customer.zip_code,
            city = customer.city,
            state = customer.state,
            card_number = customer.card_number,
            card_security_number = customer.card_security_number,
            card_expiration = customer.card_expiration,
            card_holder_name = customer.card_holder_name,
            card_type = customer.card_type,
            success_payment_count = customer.success_payment_count,
            failed_payment_count = customer.failed_payment_count,
            delivery_count = customer.delivery_count,
            data = customer.first_name,
        });

        return NotFound();
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.customerService.Cleanup();
        return Ok();
    }

    [Route("/reset")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Reset()
    {
        logger.LogWarning("Reset requested at {0}", DateTime.UtcNow);
        this.customerService.Reset();
        return Ok();
    }

}