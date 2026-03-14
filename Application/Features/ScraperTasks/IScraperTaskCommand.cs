namespace RealityScraper.Application.Features.ScraperTasks;

public interface IScraperTaskCommand
{
	string Name { get; }
	string CronExpression { get; }
	bool Enabled { get; }
	List<ScraperTaskRecipientInput> Recipients { get; }
	List<ScraperTaskTargetInput> Targets { get; }
}