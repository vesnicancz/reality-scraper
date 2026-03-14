using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class TargetInputModel
{
	public int ScraperType { get; set; }

	[Required]
	[StringLength(500)]
	[Url]
	public string Url { get; set; } = string.Empty;
}