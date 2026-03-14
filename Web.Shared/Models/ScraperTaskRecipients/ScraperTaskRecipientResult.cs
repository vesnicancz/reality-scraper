namespace RealityScraper.Web.Shared.Models.ScraperTaskRecipients;

public class ScraperTaskRecipientResult
{
	public Guid Id { get; set; }

	public Guid ScraperTaskId { get; set; }

	public required string Email { get; set; }
}