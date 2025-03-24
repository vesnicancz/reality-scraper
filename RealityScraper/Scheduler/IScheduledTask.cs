using RealityScraper.Scheduler.Configuration;

namespace RealityScraper.Scheduler;

// Rozhraní pro úlohy
public interface IScheduledTask
{
	Task ExecuteAsync(ScrapingConfiguration configuration, CancellationToken cancellationToken);
}