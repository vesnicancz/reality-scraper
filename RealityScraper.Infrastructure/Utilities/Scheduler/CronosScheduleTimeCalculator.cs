using Cronos;

namespace RealityScraper.Infrastructure.Utilities.Scheduler;

public class CronosScheduleTimeCalculator : IScheduleTimeCalculator
{
	public DateTime? GetNextExecutionTime(string cronExpression, DateTime fromTime)
	{
		if (string.IsNullOrWhiteSpace(cronExpression))
		{
			return null;
		}

		if (CronExpression.TryParse(cronExpression, out var cronExp))
		{
			return cronExp.GetNextOccurrence(fromTime, TimeZoneInfo.Local);
		}

		return null;
	}

	public bool IsValidExpression(string cronExpression)
	{
		return CronExpression.TryParse(cronExpression, out var _);
	}
}