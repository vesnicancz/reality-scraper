using System.Collections.Concurrent;
using System.Text;
using RealityScraper.Application.Interfaces.Logging;

namespace RealityScraper.Infrastructure.Logging;

public class TaskLogStore : ITaskLogStore, ITaskLogWriter
{
	private const int MaxLines = 1000;
	private const int MaxChars = 512 * 1024;

	private readonly ConcurrentDictionary<Guid, LogCapture> logs = new();

	public void StartCapture(Guid taskId)
	{
		logs[taskId] = new LogCapture();
	}

	public void Append(Guid taskId, string line)
	{
		if (logs.TryGetValue(taskId, out var capture))
		{
			capture.Append(line);
		}
	}

	public string? GetAndClear(Guid taskId)
	{
		if (logs.TryRemove(taskId, out var capture))
		{
			return capture.GetResult();
		}

		return null;
	}

	private sealed class LogCapture
	{
		private readonly StringBuilder sb = new();
		private readonly Lock lockObj = new();
		private int lineCount;
		private bool truncated;

		public void Append(string line)
		{
			lock (lockObj)
			{
				if (truncated)
				{
					return;
				}

				if (lineCount >= MaxLines || sb.Length + line.Length > MaxChars)
				{
					sb.AppendLine("--- Log byl zkrácen (max " + MaxLines + " řádků / " + MaxChars / 1024 + " KB) ---");
					truncated = true;
					return;
				}

				sb.AppendLine(line);
				lineCount++;
			}
		}

		public string GetResult()
		{
			lock (lockObj)
			{
				return sb.ToString().TrimEnd();
			}
		}
	}
}