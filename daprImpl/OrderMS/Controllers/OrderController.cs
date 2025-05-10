using System.Net;
using OnlineMarket.Core.Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using OnlineMarket.Core.OrderLibrary.Services;
using System;
using OnlineMarket.Core.OrderLibrary.Repositories;

namespace OnlineMarket.DaprImpl.OrderMS.Controllers;

[ApiController]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    private readonly IOrderService orderService;
    private readonly IOrderRepository orderRepository;

    public OrderController(IOrderService orderService, IOrderRepository orderRepository, ILogger<OrderController> logger)
    {
        this.orderService = orderService;
        this.orderRepository = orderRepository;
        this.logger = logger;
    }

    [HttpGet("{customerId}")]
    [ProducesResponseType(typeof(IEnumerable<Order>), (int)HttpStatusCode.OK)]
    public ActionResult<IEnumerable<Order>> GetByCustomerId(int customerId)
    {
        return Ok(this.orderRepository.GetByCustomerId(customerId));
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.orderService.Cleanup();
        return Ok();
    }

}

