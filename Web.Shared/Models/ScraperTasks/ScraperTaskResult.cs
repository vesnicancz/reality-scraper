using RealityScraper.Web.Shared.Models.ScraperTaskRecipients;
using RealityScraper.Web.Shared.Models.ScraperTaskTargets;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class ScraperTaskResult
{
	public Guid Id { get; set; }

	public string Name { get; set; } = null!;

	public string CronExpression { get; set; } = null!;

	public bool Enabled { get; set; }

	public DateTimeOffset? LastRunAt { get; set; }

	public DateTimeOffset? NextRunAt { get; set; }

	public List<ScraperTaskRecipientResult> Recipients { get; set; } = [];

	public List<ScraperTaskTargetResult> Targets { get; set; } = [];
}