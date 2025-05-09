using System.Net;
using OnlineMarket.Core.CartLibrary.Infra;
using OnlineMarket.Core.CartLibrary.Models;
using OnlineMarket.Core.CartLibrary.Repositories;
using OnlineMarket.Core.CartLibrary.Services;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace OnlineMarket.DaprImpl.CartMS.Controllers;

[ApiController]
public class CartController : ControllerBase
{
    private readonly ILogger<CartController> logger;
    private readonly ICartService cartService;
    private readonly ICartRepository cartRepository;
    private readonly CartConfig config;

    public CartController(ICartService cartService, ICartRepository cartRepository, IOptions<CartConfig> config, ILogger<CartController> logger)
    {
        this.cartService = cartService;
        this.cartRepository = cartRepository;
        this.config = config.Value;
        this.logger = logger;
    }

    [Route("{customerId}/add")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.MethodNotAllowed)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public ActionResult AddItem(int customerId, [FromBody] CartItem item)
    {
        if(this.config.ControllerChecks){
            if (item.Quantity <= 0)
            {
                return StatusCode((int)HttpStatusCode.MethodNotAllowed, "Item " + item.ProductId + " shows no positive quantity.");
            }

            // check if it is already on the way to checkout.... if so, cannot add product
            var cart = this.cartRepository.GetCart(customerId);
            if (cart != null && cart.status == CartStatus.CHECKOUT_SENT)
            {
                this.logger.LogWarning("Cart for customer {0} already sent for checkout.", customerId);
                return StatusCode((int)HttpStatusCode.MethodNotAllowed, "Cart for customer " + cart.customer_id + " already sent for checkout.");
            }

            if (cart is null) {
                cartRepository.Insert(new()
                {
                    customer_id = customerId,
                });
            }
        }
        CartItemModel cartItemModel = new()
        {
            customer_id = customerId,
            seller_id = item.SellerId,
            product_id = item.ProductId,
            product_name = item.ProductName,
            unit_price = item.UnitPrice,
            freight_value = item.FreightValue,
            quantity = item.Quantity,
            voucher = item.Voucher,
            version = item.Version
        };

        this.cartRepository.AddItem(cartItemModel);
        return Accepted();
    }

    [Route("{customerId}/checkout")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(Cart), (int)HttpStatusCode.MethodNotAllowed)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> NotifyCheckout(int customerId, [FromBody] CustomerCheckout customerCheckout)
    {
        if (this.config.ControllerChecks)
        {
            ObjectResult? res = null;
            if (customerId != customerCheckout.CustomerId)
            {
                logger.LogError("Customer checkout payload ({0}) does not match customer ID ({1}) in URL", customerId, customerCheckout.CustomerId);
                res = StatusCode((int)HttpStatusCode.MethodNotAllowed, "Customer checkout payload does not match customer ID in URL");
            }

            var cart = this.cartRepository.GetCart(customerCheckout.CustomerId);

            if (cart is null)
            {
                this.logger.LogWarning("Customer {0} cart cannot be found", customerCheckout.CustomerId);
                res = NotFound("Customer " + customerCheckout.CustomerId + " cart cannot be found");
            } else if (cart.status == CartStatus.CHECKOUT_SENT)
            {
                this.logger.LogWarning("Customer {0} cart has already been submitted to checkout", customerCheckout.CustomerId);
                res = StatusCode((int)HttpStatusCode.MethodNotAllowed, "Customer " + customerCheckout.CustomerId + " cart has already been submitted for checkout");
            }

            if(res is not null)
            {
                await this.cartService.ProcessPoisonCheckout(customerCheckout, Core.Common.Driver.MarkStatus.NOT_ACCEPTED);
                return res;
            }

            var items = this.cartRepository.GetItems(customerCheckout.CustomerId);
            if (items is null || items.Count == 0)
            {
                res = StatusCode((int)HttpStatusCode.MethodNotAllowed, "Customer " + customerCheckout.CustomerId + " cart has no items to be submitted for checkout");
                await this.cartService.ProcessPoisonCheckout(customerCheckout, Core.Common.Driver.MarkStatus.NOT_ACCEPTED);
                return res;
            }
        }
        try
        {
            await this.cartService.NotifyCheckout(customerCheckout);
            // this.logger.LogWarning("Checkout requested for customer "+customerCheckout.CustomerId);
            return Accepted();
        }
        catch (Exception e)
        {
            await this.cartService.ProcessPoisonCheckout(customerCheckout, Core.Common.Driver.MarkStatus.ABORT);
            return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
        }
    }

    [HttpGet("{customerId}")]
    [ProducesResponseType(typeof(Cart), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult<Cart> Get(int customerId)
    {
        var cart = this.cartRepository.GetCart(customerId);
        if (cart is null)
            return NotFound();

        var items = cartRepository.GetItems(customerId);

        // https://stackoverflow.com/questions/9524682/

        var cartItems = items.Select(i => new CartItem()
        {
            SellerId = i.seller_id,
            ProductId = i.product_id,
            ProductName = i.product_name is null ? "" : i.product_name,
            UnitPrice = i.unit_price,
            FreightValue = i.freight_value,
            Quantity = i.quantity,
            Voucher = i.voucher
        }).ToList();

        return Ok(new Cart()
        {
            customerId = cart.customer_id,
            items = cartItems,
            status = cart.status,
        });
    }

    [HttpDelete("{customerId}")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult Delete(int customerId)
    {
        this.logger.LogInformation("Customer {0} requested to delete cart.", customerId);
        var cart = this.cartRepository.Delete(customerId);
        if (cart is null)
            return NotFound();
        return Accepted();
    }

    [Route("{customerId}/seal")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public ActionResult Seal(int customerId)
    {
         if (config.ControllerChecks)
         {
            var cart = this.cartRepository.GetCart(customerId);
            if(cart is null)
            {
               return NotFound();
            }
             
            /**
                * Seal is a terminal state, so no need to check for concurrent operation
                */
            this.cartService.Seal(cart);
            return Accepted();
         }
         return BadRequest("Seal cannot work without controller checks options on");
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.cartService.Cleanup();
        return Ok();
    }

    [Route("/reset")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Reset()
    {
        logger.LogWarning("Reset requested at {0}", DateTime.UtcNow);
        this.cartService.Reset();
        return Ok();
    }

}