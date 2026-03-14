namespace RealityScraper.Application.Interfaces.Scheduler;

public interface ISchedulerRefreshSignal
{
	void RequestRefresh();

	Task<bool> WaitForRefreshAsync(TimeSpan timeout, CancellationToken cancellationToken);
}