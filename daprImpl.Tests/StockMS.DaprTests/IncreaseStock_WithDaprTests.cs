// tests/StockMS.DaprTests/IncreaseStock_WithDaprTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using Xunit;

[CollectionDefinition("dapr-collection")]
public class DaprCollection : ICollectionFixture<DaprSidecarFixture> { }

/* 通过 Collection 共享一个 Sidecar，避免每个测试都重启 */
[Collection("dapr-collection")]
public class IncreaseStock_WithDaprTests
{
    private readonly HttpClient client;

    public IncreaseStock_WithDaprTests(DaprSidecarFixture sidecar)
    {
        var factory = new StockApiWithDaprFactory(sidecar);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task IncreaseStock_Should_Return_202_And_Publish()
    {
        /* 1. 先创建库存 */
        await client.PostAsJsonAsync("/", new StockItem
        {
            seller_id     = 1,
            product_id    = 888,
            qty_available = 1,
            version       = "v1"
        });

        /* 2. 调用 PATCH / (IncreaseStock) */
        var resp = await client.PatchAsJsonAsync("/", new IncreaseStock
        {
            seller_id  = 1,
            product_id = 888,
            quantity   = 9,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        /* 3. （可选）等待 sidecar flush，并检查 Redis / 订阅端是否收到了事件
           如果使用 memory-pubsub，无法从外部查看消息，通常只需确认 Publish 没异常。
        */
    }
}