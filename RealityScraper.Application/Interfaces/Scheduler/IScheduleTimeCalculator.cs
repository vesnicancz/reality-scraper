namespace RealityScraper.Application.Interfaces.Scheduler;

public interface IScheduleTimeCalculator
{
	DateTime? GetNextExecutionTime(string cronExpression, DateTime fromTime);

	bool IsValidExpression(string cronExpression);
}