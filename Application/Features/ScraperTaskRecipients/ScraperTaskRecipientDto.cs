namespace RealityScraper.Application.Features.ScraperTaskRecipients;

public class ScraperTaskRecipientDto
{
	public Guid Id { get; set; }

	public Guid ScraperTaskId { get; set; }

	public string Email { get; set; }
}