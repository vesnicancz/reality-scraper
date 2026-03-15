namespace RealityScraper.Application.Features.Scheduler;

public sealed record TaskExecutionResult(
	DateTimeOffset LastRunTime,
	DateTimeOffset? NextRunTime,
	string? LastRunLog,
	bool Succeeded);