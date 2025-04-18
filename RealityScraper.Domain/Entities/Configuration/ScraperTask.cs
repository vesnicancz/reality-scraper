using RealityScraper.Domain.Common;

namespace RealityScraper.Domain.Entities.Configuration;

public class ScraperTask : BaseEntity
{
	public string Name { get; set; }

	public string CronExpression { get; set; }

	public bool Enabled { get; set; } = true;

	public DateTime CreatedAt { get; set; }

	public DateTime? LastRunAt { get; set; }

	public DateTime? NextRunAt { get; set; }

	// Navigační vlastnosti
	public List<ScraperRecipient> Recipients { get; set; } = new List<ScraperRecipient>();

	public List<ScraperTaskTarget> Targets { get; set; } = new List<ScraperTaskTarget>();
}