using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public abstract class ScraperTaskInputModel
{
	[Required(ErrorMessage = "Název je povinný.")]
	[StringLength(100, ErrorMessage = "Název může mít maximálně 100 znaků.")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "Cron výraz je povinný.")]
	[StringLength(50, ErrorMessage = "Cron výraz může mít maximálně 50 znaků.")]
	public string CronExpression { get; set; } = string.Empty;

	public bool Enabled { get; set; } = true;

	public List<RecipientInputModel> Recipients { get; set; } = [];

	public List<TargetInputModel> Targets { get; set; } = [];
}