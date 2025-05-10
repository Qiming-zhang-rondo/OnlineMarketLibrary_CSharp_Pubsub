namespace OnlineMarket.Core.SellerLibrary.Infra;

public sealed class SellerConfig
{
    public bool PostgresEmbed { get; set; } = false;

    public bool InMemoryDb { get; set; } = false;

    public bool Logging { get; set; } = false;

    public int LoggingDelay { get; set; } = 10000;

    public string RamDiskDir { get; set; } = "";
}