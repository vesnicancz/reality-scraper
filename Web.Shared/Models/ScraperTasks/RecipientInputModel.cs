using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public class RecipientInputModel
{
	[Required]
	[StringLength(100)]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;
}