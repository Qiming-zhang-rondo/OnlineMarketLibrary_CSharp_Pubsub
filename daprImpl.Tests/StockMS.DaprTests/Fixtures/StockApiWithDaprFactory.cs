// tests/StockMS.DaprTests/Fixtures/StockApiWithDaprFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OnlineMarket.Core.StockLibrary.Infra;
using OnlineMarket.Common.Messaging;

public sealed class StockApiWithDaprFactory : WebApplicationFactory<Program>
{
    private readonly DaprSidecarFixture sidecar;
    public StockApiWithDaprFactory(DaprSidecarFixture sidecar) => this.sidecar = sidecar;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1) 告诉被测服务 Dapr 端口
        builder.UseSetting("DAPR_HTTP_PORT", sidecar.HttpPort.ToString());
        builder.UseSetting("DAPR_GRPC_PORT", sidecar.GrpcPort.ToString());

        builder.ConfigureServices(svcs =>
        {
            // 2) 换成真正的 DaprEventPublisher
            svcs.RemoveAll(typeof(IEventPublisher));
            svcs.AddSingleton<IEventPublisher, DaprEventPublisher>();

            // 3) 打开 Streaming
            svcs.Configure<StockConfig>(opt => opt.Streaming = true);
        });
    }
}