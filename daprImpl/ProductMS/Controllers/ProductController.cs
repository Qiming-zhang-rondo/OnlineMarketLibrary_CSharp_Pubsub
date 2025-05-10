using System.Net;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.ProductLibrary.Repositories;
using OnlineMarket.Core.ProductLibrary.Services;


namespace OnlineMarket.DaprImpl.ProductMS.Controllers;

[ApiController]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> logger;
    private readonly IProductRepository productRepository;
    private readonly IProductService productService;

    public ProductController(IProductService productService, IProductRepository productRepository, ILogger<ProductController> logger)
    {
        this.productService = productService;
        this.logger = logger;
        this.productRepository = productRepository;
    }

    [HttpGet("{sellerId}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(List<Product>), (int)HttpStatusCode.OK)]
    public ActionResult<List<Product>> GetBySellerId(int sellerId)
    {
        this.logger.LogInformation("[GetBySeller] received for seller {0}", sellerId);
        if (sellerId <= 0)
        {
            return BadRequest();
        }
        var products = this.productRepository.GetBySeller(sellerId);

        if (products is not null && products.Count() > 0)
        {
            List<Product> productsResp = new List<Product>(products.Count());
            foreach(var product in products)
            {
                if (!product.active) continue;
                productsResp.Add(
                new Product()
                {
                    seller_id = product.seller_id,
                    product_id = product.product_id,
                    name = product.name,
                    sku = product.sku,
                    category = product.category,
                    description = product.description,
                    price = product.price,
                    freight_value = product.freight_value,
                    status = product.status
                });
            }
            this.logger.LogInformation("[GetBySeller] returning seller {0} products...", sellerId);
            return Ok(productsResp);
        }
        return NotFound();
    }

    [HttpGet("{sellerId:int}/{productId:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(Product), (int)HttpStatusCode.OK)]
    public ActionResult<Product> GetBySellerIdAndProductId(int sellerId, int productId)
    {
        this.logger.LogInformation("[GetBySellerIdAndProductId] received for product {0}", productId);
        if (productId <= 0)
        {
            return BadRequest("Product ID is not valid");
        }
        var product = this.productRepository.GetProduct(sellerId, productId);

        if (product is null)
        {
            return NotFound("Product is null");
        }
        
        this.logger.LogInformation("[GetById] returning product {0}", productId);
        return Ok(new Product()
        {
            seller_id = product.seller_id,
            product_id= product.product_id,
            name = product.name,
            sku = product.sku,
            category = product.category,
            description = product.description,
            price = product.price,
            freight_value = product.freight_value,
            status = product.status,
            version = product.version
        });
        
    }

    [HttpPost]
    [Route("/")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public ActionResult AddProduct([FromBody] Product product)
    {
        this.productService.ProcessCreateProduct(product);
        return StatusCode((int)HttpStatusCode.Created);
    }

    [HttpPut]
    [Route("/")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<ActionResult> UpdateProduct([FromBody] Product product)
    {
        try {
            await this.productService.ProcessProductUpdate(product);
        } catch(Exception e)
        {
            logger.LogError(e.ToString());
            await this.productService.ProcessPoisonProductUpdate(product);
        }
        return Ok();
    }

    [HttpPatch]
    [Route("/")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<ActionResult> UpdateProductPrice([FromBody] PriceUpdate update)
    {
        try{
            await this.productService.ProcessPriceUpdate(update);
        } catch(Exception e)
        {
            logger.LogError(e.ToString());
            await this.productService.ProcessPoisonPriceUpdate(update);
        }
        return Accepted();
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.productService.Cleanup();
        return Ok();
    }

    [Route("/reset")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Reset()
    {
        logger.LogWarning("Reset requested at {0}", DateTime.UtcNow);
        this.productService.Reset();
        return Ok();
    }

}