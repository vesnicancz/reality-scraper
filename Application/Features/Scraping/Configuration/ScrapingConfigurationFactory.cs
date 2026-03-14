using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Features.Scraping.Configuration;

public static class ScrapingConfigurationFactory
{
	public static ScrapingConfiguration CreateFromTask(ScraperTask task)
	{
		return new ScrapingConfiguration
		{
			Id = task.Id,
			Name = task.Name,
			EmailRecipients = task.Recipients?.Select(r => r.Email).ToList() ?? new List<string>(),
			Scrapers = task.Targets?.Select(t => new ScraperConfiguration
			{
				ScraperType = t.ScraperType,
				Url = t.Url
			}).ToList() ?? new List<ScraperConfiguration>()
		};
	}
}