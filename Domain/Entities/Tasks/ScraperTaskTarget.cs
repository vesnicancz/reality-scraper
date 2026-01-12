using RealityScraper.Domain.Enums;
using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Tasks;

public class ScraperTaskTarget : Entity
{
	public Guid ScraperTaskId { get; protected set; }

	public ScraperTask ScraperTask { get; protected set; }

	public ScrapersEnum ScraperType { get; protected set; }

	public string Url { get; protected set; }

	protected ScraperTaskTarget()
	{
	}

	public ScraperTaskTarget(ScrapersEnum scraperType, string url)
	{
		ScraperType = scraperType;
		Url = url;
	}

	public void SetScraperTask(ScraperTask scraperTask)
	{
		ScraperTaskId = scraperTask.Id;
		ScraperTask = scraperTask;
	}
}