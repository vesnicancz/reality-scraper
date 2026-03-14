using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class SchedulerRefreshSignal : ISchedulerRefreshSignal
{
	private readonly SemaphoreSlim semaphore = new(0, 1);

	public void RequestRefresh()
	{
		try
		{
			semaphore.Release();
		}
		catch (SemaphoreFullException)
		{
			// Signal already pending, no action needed
		}
	}

	public async Task<bool> WaitForRefreshAsync(TimeSpan timeout, CancellationToken cancellationToken)
	{
		// Returns true if signaled, false if timed out
		return await semaphore.WaitAsync(timeout, cancellationToken);
	}
}