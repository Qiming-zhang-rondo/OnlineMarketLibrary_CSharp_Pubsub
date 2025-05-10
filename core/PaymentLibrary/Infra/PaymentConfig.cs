namespace OnlineMarket.Core.PaymentLibrary.Infra;

public class PaymentConfig
{
	public bool PaymentProvider { get; set; } = false;

	public string PaymentProviderUrl { get; set; } = "";

	public bool Streaming { get; set; } = false;

	public bool InMemoryDb { get; set; } = false;

	public bool PostgresEmbed { get; set; } = false;

    public bool Logging { get; set; } = false;

    public int LoggingDelay { get; set; } = 10000;

    public string RamDiskDir { get; set; } = "";

	public int Delay { get; set; } = 0;
}

