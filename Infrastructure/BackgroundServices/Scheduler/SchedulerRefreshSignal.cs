using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class SchedulerRefreshSignal : ISchedulerRefreshSignal
{
	private readonly SemaphoreSlim semaphore = new(0);

	public void RequestRefresh()
	{
		// Release only if no one is already waiting with a pending signal
		if (semaphore.CurrentCount == 0)
		{
			semaphore.Release();
		}
	}

	public async Task<bool> WaitForRefreshAsync(TimeSpan timeout, CancellationToken cancellationToken)
	{
		// Returns true if signaled, false if timed out
		return await semaphore.WaitAsync(timeout, cancellationToken);
	}
}