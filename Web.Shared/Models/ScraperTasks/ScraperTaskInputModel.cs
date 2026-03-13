namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public abstract class ScraperTaskInputModel
{
	public string Name { get; set; } = string.Empty;

	public string CronExpression { get; set; } = string.Empty;

	public bool Enabled { get; set; } = true;

	public List<RecipientInputModel> Recipients { get; set; } = [];

	public List<TargetInputModel> Targets { get; set; } = [];
}