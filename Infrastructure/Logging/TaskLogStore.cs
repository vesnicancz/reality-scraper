using System.Collections.Concurrent;
using System.Text;
using RealityScraper.Application.Interfaces.Logging;

namespace RealityScraper.Infrastructure.Logging;

public class TaskLogStore : ITaskLogStore, ITaskLogWriter
{
	private const int MaxLines = 1000;

	private readonly ConcurrentDictionary<Guid, (StringBuilder sb, int lineCount)> logs = new();

	public void StartCapture(Guid taskId)
	{
		logs[taskId] = (new StringBuilder(), 0);
	}

	public void Append(Guid taskId, string line)
	{
		if (logs.TryGetValue(taskId, out var entry))
		{
			lock (entry.sb)
			{
				if (entry.lineCount < MaxLines)
				{
					entry.sb.AppendLine(line);
					logs[taskId] = (entry.sb, entry.lineCount + 1);
				}
				else if (entry.lineCount == MaxLines)
				{
					entry.sb.AppendLine("--- Log byl zkrácen (max " + MaxLines + " řádků) ---");
					logs[taskId] = (entry.sb, entry.lineCount + 1);
				}
			}
		}
	}

	public string? GetAndClear(Guid taskId)
	{
		if (logs.TryRemove(taskId, out var entry))
		{
			lock (entry.sb)
			{
				return entry.sb.ToString().TrimEnd();
			}
		}

		return null;
	}
}