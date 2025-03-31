using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scheduling.Configuration;

public class ScraperConfiguration
{
	public ScrapersEnum ScraperType { get; set; }

	public string Url { get; set; }
}