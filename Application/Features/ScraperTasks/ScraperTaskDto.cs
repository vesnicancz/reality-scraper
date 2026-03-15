using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskTargets;

namespace RealityScraper.Application.Features.ScraperTasks;

public class ScraperTaskDto
{
	public Guid Id { get; set; }

	public string Name { get; set; } = null!;

	public string CronExpression { get; set; } = null!;

	public bool Enabled { get; set; }

	public DateTimeOffset? LastRunAt { get; set; }

	public DateTimeOffset? NextRunAt { get; set; }

	public bool? LastRunSucceeded { get; set; }

	public string? LastRunLog { get; set; }

	public List<ScraperTaskRecipientDto> Recipients { get; set; } = [];

	public List<ScraperTaskTargetDto> Targets { get; set; } = [];
}