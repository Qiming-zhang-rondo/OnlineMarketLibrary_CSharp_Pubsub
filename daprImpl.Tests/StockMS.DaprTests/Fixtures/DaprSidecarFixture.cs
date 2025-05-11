// tests/StockMS.DaprTests/Fixtures/DaprSidecarFixture.cs
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

public sealed class DaprSidecarFixture : IAsyncLifetime
{
    public int HttpPort { get; } = GetFreePort();
    public int GrpcPort { get; } = GetFreePort();
    private Process? sidecar;

    public async Task InitializeAsync()
    {
        // 1) 启动 dapr run
        var args =
            $"run --app-id stock-test " +
            $"--dapr-http-port {HttpPort} --dapr-grpc-port {GrpcPort} " +
            $"--components-path \"{ComponentPath}\" --resources-path \"{ComponentPath}\" " +
            "--log-level warn --app-port 0";               // app-port 0 = 不反向探活

        sidecar = Process.Start(new ProcessStartInfo("dapr", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        });

        // 2) 等待 “sidecar started” 关键字
        var readyRegex = new Regex("Finished starting.*dapr.*stock-test", RegexOptions.IgnoreCase);
        while (!sidecar!.HasExited && !readyRegex.IsMatch(await sidecar.StandardError.ReadLineAsync() ?? ""))
        { /* 等待 */ }
    }

    public Task DisposeAsync()
    {
        if (sidecar is { HasExited: false }) sidecar.Kill(true);
        return Task.CompletedTask;
    }

    /* ---------- 工具 ---------- */
    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string ComponentPath => Path.Combine(
        Directory.GetCurrentDirectory(),                    // tests/bin/Debug/netX/
        "components");                                      // 拷贝 pubsub.yaml 进去
}