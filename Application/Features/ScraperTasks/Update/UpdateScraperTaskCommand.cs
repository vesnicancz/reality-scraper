using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks.Create;

namespace RealityScraper.Application.Features.ScraperTasks.Update;

public record UpdateScraperTaskCommand(
	Guid Id,
	string Name,
	string CronExpression,
	bool Enabled,
	List<CreateScraperTaskRecipientInput> Recipients,
	List<CreateScraperTaskTargetInput> Targets) : ICommand<ScraperTaskDto>;