namespace RealityScraper.Application.Features.ScraperTasks;

public class ScraperTaskDto
{
	public Guid Id { get; set; }

	public string Name { get; set; } = null!;

	public string CronExpression { get; set; } = null!;

	public bool Enabled { get; set; }
}