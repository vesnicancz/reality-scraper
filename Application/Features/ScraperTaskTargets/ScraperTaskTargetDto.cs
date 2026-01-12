namespace RealityScraper.Application.Features.ScraperTaskTargets;

public class ScraperTaskTargetDto
{
	public Guid Id { get; set; }

	public Guid ScraperTaskId { get; set; }

	public int ScraperType { get; set; }

	public string Url { get; set; }
}