using RealityScraper.Domain.Common;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Domain.Entities.Configuration;

public class ScraperTaskTarget : BaseEntity
{
	public Guid ScraperTaskId { get; set; }

	public ScraperTask ScraperTask { get; set; }

	public ScrapersEnum ScraperType { get; set; }

	public string Url { get; set; }
}