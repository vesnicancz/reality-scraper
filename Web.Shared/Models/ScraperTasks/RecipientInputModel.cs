using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class RecipientInputModel
{
	[Required(ErrorMessage = "E-mail je povinný.")]
	[StringLength(100, ErrorMessage = "E-mail může mít maximálně 100 znaků.")]
	[EmailAddress(ErrorMessage = "Neplatný formát e-mailu.")]
	public string Email { get; set; } = string.Empty;
}