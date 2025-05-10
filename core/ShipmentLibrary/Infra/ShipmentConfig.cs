namespace OnlineMarket.Core.ShipmentLibrary.Infra;

public class ShipmentConfig
{
    public bool Streaming { get; set; }

    public bool InMemoryDb { get; set; } = false;

    public bool PostgresEmbed { get; set; } = false;

    public bool Logging { get; set; } = false;

    public int LoggingDelay { get; set; } = 10000;

    public string RamDiskDir { get; set; } = "";
}