namespace RealityScraper.Infrastructure.Configuration;

public class SmtpOptions
{
	public required string Server { get; set; }

	public int Port { get; set; }

	public required string Username { get; set; }

	public required string Password { get; set; }

	public bool EnableSsl { get; set; }

	public required string FromAddress { get; set; }

	public required string FromName { get; set; }
}