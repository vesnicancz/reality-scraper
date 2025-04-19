namespace RealityScraper.Infrastructure.Configuration;

public class SeleniumOptions
{
	public int PageLoadTimeoutSeconds { get; set; } = 30;

	public string DriverPath { get; set; } = "./drivers";

	public List<string>? BrowserArguments { get; set; }

	public string? ProxyUrl { get; set; }

	public string? UserAgent { get; set; }

	public bool UseRemoteDriver { get; set; }

	public string SeleniumHubUrl { get; set; } = "http://localhost:4444/wd/hub";
}