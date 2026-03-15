using System.Collections.Concurrent;
using System.Text;
using RealityScraper.Application.Interfaces.Logging;

namespace RealityScraper.Infrastructure.Logging;

public class TaskLogStore : ITaskLogStore, ITaskLogWriter
{
	private readonly ConcurrentDictionary<Guid, StringBuilder> logs = new();

	public void StartCapture(Guid taskId)
	{
		logs[taskId] = new StringBuilder();
	}

	public void Append(Guid taskId, string line)
	{
		if (logs.TryGetValue(taskId, out var sb))
		{
			lock (sb)
			{
				sb.AppendLine(line);
			}
		}
	}

	public string? GetAndClear(Guid taskId)
	{
		if (logs.TryRemove(taskId, out var sb))
		{
			lock (sb)
			{
				return sb.ToString().TrimEnd();
			}
		}

		return null;
	}
}