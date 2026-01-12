namespace RealityScraper.Web.Shared.Models.ScraperTaskTargets;

public class CreateScraperTaskTargetRequest
{
	public Guid ScraperTaskId { get; set; }

	public int ScraperType { get; set; }

	public string Url { get; set; }
}