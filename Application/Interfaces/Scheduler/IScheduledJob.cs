namespace RealityScraper.Application.Interfaces.Scheduler;

/// <summary>
/// Plánovaná úloha spouštěná schedulerem. Implementace si podle ID
/// načte vlastní konfiguraci z databáze.
/// </summary>
public interface IScheduledJob
{
	Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken);
}
