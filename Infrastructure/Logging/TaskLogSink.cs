using System.Globalization;
using RealityScraper.Application.Interfaces.Logging;
using Serilog.Core;
using Serilog.Events;

namespace RealityScraper.Infrastructure.Logging;

public class TaskLogSink : ILogEventSink
{
	private readonly ITaskLogWriter taskLogWriter;

	public TaskLogSink(ITaskLogWriter taskLogWriter)
	{
		this.taskLogWriter = taskLogWriter;
	}

	public void Emit(LogEvent logEvent)
	{
		if (!logEvent.Properties.TryGetValue("TaskId", out var taskIdProperty))
		{
			return;
		}

		if (taskIdProperty is not ScalarValue scalarValue || scalarValue.Value is not Guid taskId)
		{
			return;
		}

		var level = logEvent.Level switch
		{
			LogEventLevel.Verbose => "VRB",
			LogEventLevel.Debug => "DBG",
			LogEventLevel.Information => "INF",
			LogEventLevel.Warning => "WRN",
			LogEventLevel.Error => "ERR",
			LogEventLevel.Fatal => "FTL",
			_ => "???"
		};

		var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
		var line = $"{logEvent.Timestamp:yyyy-MM-dd HH:mm:sszzz} [{level}] {message}";
		taskLogWriter.Append(taskId, line);

		if (logEvent.Exception != null)
		{
			foreach (var exceptionLine in logEvent.Exception.ToString().Split(Environment.NewLine))
			{
				taskLogWriter.Append(taskId, exceptionLine);
			}
		}
	}
}