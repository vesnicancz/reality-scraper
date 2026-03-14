using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class TargetInputModel
{
	public int ScraperType { get; set; }

	[Required(ErrorMessage = "URL je povinná.")]
	[StringLength(500, ErrorMessage = "URL může mít maximálně 500 znaků.")]
	[Url(ErrorMessage = "Neplatný formát URL.")]
	public string Url { get; set; } = string.Empty;
}