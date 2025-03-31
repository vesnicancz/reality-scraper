using RealityScraper.Application.Features.Scheduling.Configuration;

namespace RealityScraper.Application.Features.Scheduling;

// Rozhraní pro úlohy
public interface IScheduledTask
{
	Task ExecuteAsync(ScrapingConfiguration configuration, CancellationToken cancellationToken);
}