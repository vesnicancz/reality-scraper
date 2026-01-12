using Cronos;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Infrastructure.Utilities.Scheduler;

public class CronosScheduleTimeCalculator : IScheduleTimeCalculator
{
	public DateTimeOffset? GetNextExecutionTime(string cronExpression, DateTimeOffset fromTime)
	{
		if (string.IsNullOrWhiteSpace(cronExpression))
		{
			return null;
		}

		if (CronExpression.TryParse(cronExpression, out var cronExp))
		{
			return cronExp.GetNextOccurrence(fromTime, TimeZoneInfo.Utc);
		}

		return null;
	}

	public bool IsValidExpression(string cronExpression)
	{
		return CronExpression.TryParse(cronExpression, out var _);
	}
}