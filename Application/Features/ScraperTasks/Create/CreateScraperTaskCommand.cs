using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ScraperTasks.Create;

public record CreateScraperTaskCommand(
	string Name,
	string CronExpression,
	bool Enabled,
	List<CreateScraperTaskRecipientInput> Recipients,
	List<CreateScraperTaskTargetInput> Targets) : ICommand<ScraperTaskDto>;

public record CreateScraperTaskRecipientInput(string Email);

public record CreateScraperTaskTargetInput(int ScraperType, string Url);