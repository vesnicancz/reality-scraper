namespace RealityScraper.Application.Interfaces.Logging;

public interface ITaskLogWriter
{
	void Append(Guid taskId, string line);
}