// using System.Net;
// using System.Net.Http.Json;
// using Microsoft.AspNetCore.Mvc.Testing;
// using OnlineMarket.DaprImpl.ProductMS;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Requests;
// using Xunit;

// namespace daprImpl.Tests
// {
//     public class ProductTests : IClassFixture<WebApplicationFactory<Program>>
//     {
//         private readonly HttpClient _client;

//         public ProductTests(WebApplicationFactory<Program> factory)
//         {
//             _client = factory.WithWebHostBuilder(builder =>
//             {
//                 // 如果你想 override 配置，这里加
//             }).CreateClient();
//         }

//         [Fact]
//         public async Task UpdateProduct_ReturnsOk()
//         {
//             var product = new Product
//             {
//                 seller_id = 1,
//                 product_id = 1001,
//                 name = "Updated Product",
//                 sku = "SKU-1001",
//                 category = "UpdatedCat",
//                 description = "This is an updated product",
//                 price = 19.99f,
//                 freight_value = 2.5f,
//                 status = "Active",
//                 version = "v2"
//             };

//             var response = await _client.PutAsJsonAsync("/", product);

//             Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         }

//         [Fact]
//         public async Task UpdateProductPrice_ReturnsAccepted()
//         {
//             var priceUpdate = new PriceUpdate
//             {
//                 seller_id = 1,
//                 product_id = 1001,
//                 price = 15.99f,
//                 freight_value = 2.0f,
//                 version = "v3"
//             };

//             var response = await _client.PatchAsJsonAsync("/", priceUpdate);

//             Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
//         }
//     }
// }