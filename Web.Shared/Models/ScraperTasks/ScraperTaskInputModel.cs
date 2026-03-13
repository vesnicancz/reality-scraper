namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public abstract class ScraperTaskInputModel
{
	public string Name { get; set; }

	public string CronExpression { get; set; }

	public bool Enabled { get; set; } = true;

	public List<RecipientInputModel> Recipients { get; set; } = [];

	public List<TargetInputModel> Targets { get; set; } = [];
}