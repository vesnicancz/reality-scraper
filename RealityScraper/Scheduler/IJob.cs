namespace RealityScraper.Scheduler;

// Rozhraní pro úlohu
public interface IJob
{
	Task ExecuteAsync(CancellationToken cancellationToken);
}