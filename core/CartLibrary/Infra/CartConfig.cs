namespace OnlineMarket.Core.CartLibrary.Infra;

/**
 * https://stackoverflow.com/questions/31453495/how-to-read-appsettings-values-from-a-json-file-in-asp-net-core
 */
public class CartConfig
{
    public bool ControllerChecks { get; set; }

    public bool Streaming { get; set; }

    public bool InMemoryDb { get; set; } = false;

    public bool PostgresEmbed { get; set; } = false;

    public bool Logging { get; set; } = false;

    public int LoggingDelay { get; set; } = 10000;

    public string RamDiskDir { get; set; } = "";

}
