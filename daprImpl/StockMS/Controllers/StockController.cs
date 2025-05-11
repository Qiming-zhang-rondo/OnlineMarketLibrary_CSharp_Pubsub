using System.Net;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.StockLibrary.Models;
using OnlineMarket.Core.StockLibrary.Repositories;
using OnlineMarket.Core.StockLibrary.Services;


namespace OnlineMarket.DaprImpl.StockMS.Controllers;

[ApiController]
public class StockController : ControllerBase
{
    private readonly IStockService stockService;

    private readonly IStockRepository stockRepository;

    private readonly ILogger<StockController> logger;

    public StockController(IStockService stockService, IStockRepository stockRepository, ILogger<StockController> logger)
    {
        this.stockService = stockService;
        this.stockRepository = stockRepository;
        this.logger = logger;
    }

    [HttpPatch("/")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> IncreaseStock([FromBody] IncreaseStock increaseStock)
    {
        this.logger.LogInformation("[IncreaseStock] received for item id {0}", increaseStock.product_id);
        var item = this.stockRepository.Find(increaseStock.seller_id, increaseStock.product_id);
        if (item is null)
        {
            this.logger.LogInformation("[IncreaseStock] completed for item id {0}.", increaseStock.product_id);
            return NotFound();
        }
        await this.stockService.IncreaseStock(increaseStock);
        return Accepted();
    }

    [HttpPost("/")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult> AddStockItem([FromBody] StockItem stockItem)
    {
        this.logger.LogInformation("[AddStockItem] received for item id {0}", stockItem.product_id);
        Task t = this.stockService.CreateStockItem(stockItem);
        await t;
        if (t.IsCompletedSuccessfully)
        {
            this.logger.LogInformation("[AddStockItem] completed for item id {0}.", stockItem.product_id);
            return StatusCode((int)HttpStatusCode.Created);
        }
        return StatusCode((int)HttpStatusCode.InternalServerError);
    }

    [HttpGet("{sellerId:int}/{productId:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(StockItem), (int)HttpStatusCode.OK)]
    public ActionResult<StockItem> GetBySellerIdAndProductId(int sellerId, int productId)
    {
        StockItemModel? item = this.stockRepository.Find(sellerId, productId);
        if (item is not null){
            return Ok(new StockItem()
            {
                seller_id = item.seller_id,
                product_id = item.product_id,
                qty_available = item.qty_available,
                qty_reserved = item.qty_reserved,
                order_count = item.order_count,
                ytd = item.ytd,
                data = item.data,
                version = item.version
            });
        }
        return NotFound();
    }

    [HttpGet("{sellerId}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(List<StockItemModel>), (int)HttpStatusCode.OK)]
    public ActionResult<List<StockItemModel>> GetBySellerId(int sellerId)
    {
        this.logger.LogInformation("[GetBySeller] received for seller {0}", sellerId);
        if (sellerId <= 0)
        {
            return BadRequest();
        }
        var items = this.stockRepository.GetBySellerId(sellerId);

        if (items is not null && items.Count() > 0)
        {
            List<StockItem> itemsResponse = new List<StockItem>(items.Count());
            foreach (var item in items)
            {
                itemsResponse.Add(
                new StockItem()
                {
                    seller_id = item.seller_id,
                    product_id = item.product_id,
                    qty_available = item.qty_available,
                    qty_reserved = item.qty_reserved,
                    order_count = item.order_count,
                    ytd = item.ytd,
                    data = item.data
                });
            }
            this.logger.LogInformation("[GetBySellerId] returning seller {0} items...", sellerId);
            return Ok(itemsResponse);
        }
        return NotFound();
    }

    [Route("/cleanup")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Cleanup()
    {
        logger.LogWarning("Cleanup requested at {0}", DateTime.UtcNow);
        this.stockService.Cleanup();
        return Ok();
    }

    [Route("/reset")]
    [HttpPatch]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public ActionResult Reset()
    {
        logger.LogWarning("Reset requested at {0}", DateTime.UtcNow);
        this.stockService.Reset();
        return Ok();
    }

}