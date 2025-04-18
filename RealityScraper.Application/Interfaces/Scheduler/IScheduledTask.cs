using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Application.Features.Scheduling;

// Rozhraní pro úlohy
public interface IScheduledTask
{
	Task ExecuteAsync(ScrapingConfiguration configuration, CancellationToken cancellationToken);
}