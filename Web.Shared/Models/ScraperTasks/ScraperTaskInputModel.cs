using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public abstract class ScraperTaskInputModel
{
	[Required]
	[StringLength(100)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string CronExpression { get; set; } = string.Empty;

	public bool Enabled { get; set; } = true;

	public List<RecipientInputModel> Recipients { get; set; } = [];

	public List<TargetInputModel> Targets { get; set; } = [];
}