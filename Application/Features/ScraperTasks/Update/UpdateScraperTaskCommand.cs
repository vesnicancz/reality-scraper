using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.Update;

public record UpdateScraperTaskCommand(
	Guid Id,
	string Name,
	string CronExpression,
	bool Enabled,
	List<ScraperTaskRecipientInput> Recipients,
	List<ScraperTaskTargetInput> Targets) : ICommand<ScraperTaskDto>, IScraperTaskCommand;
