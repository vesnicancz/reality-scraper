namespace RealityScraper.Application.Features.ScraperTaskRecipients;

public class ScraperTaskRecipientDto
{
	public Guid Id { get; set; }

	public Guid ScraperTaskId { get; set; }

	public required string Email { get; set; }
}