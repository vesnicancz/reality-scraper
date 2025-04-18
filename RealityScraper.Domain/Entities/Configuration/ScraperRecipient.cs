using RealityScraper.Domain.Common;

namespace RealityScraper.Domain.Entities.Configuration;

public class ScraperRecipient : BaseEntity
{
	public Guid ScraperTaskId { get; set; }

	public ScraperTask ScraperTask { get; set; }

	public string Email { get; set; }
}