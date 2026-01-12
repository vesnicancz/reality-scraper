namespace RealityScraper.Web.Shared.Models.ScraperTaskRecipients;

public class CreateScraperTaskRecipientRequest
{
	public Guid ScraperTaskId { get; set; }

	public string Email { get; set; }
}