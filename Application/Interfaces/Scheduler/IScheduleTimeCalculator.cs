namespace RealityScraper.Application.Interfaces.Scheduler;

public interface IScheduleTimeCalculator
{
	DateTimeOffset? GetNextExecutionTime(string cronExpression, DateTimeOffset fromTime);

	bool IsValidExpression(string cronExpression);
}