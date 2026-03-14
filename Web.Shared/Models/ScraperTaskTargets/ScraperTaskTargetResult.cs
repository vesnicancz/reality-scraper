namespace RealityScraper.Web.Shared.Models.ScraperTaskTargets;

public class ScraperTaskTargetResult
{
	public Guid Id { get; set; }

	public Guid ScraperTaskId { get; set; }

	public int ScraperType { get; set; }

	public required string Url { get; set; }
}