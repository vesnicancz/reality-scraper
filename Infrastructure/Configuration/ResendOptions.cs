namespace RealityScraper.Infrastructure.Configuration;

public class ResendOptions
{
	public required string ApiKey { get; set; }

	public required string FromEmail { get; set; }

	public required string FromName { get; set; }
}