namespace RealityScraper.Application.Interfaces.Logging;

public interface ITaskLogStore
{
	void StartCapture(Guid taskId);

	string? GetAndClear(Guid taskId);
}