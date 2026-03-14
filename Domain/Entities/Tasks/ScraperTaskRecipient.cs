using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Tasks;

public class ScraperTaskRecipient : Entity
{
	public Guid ScraperTaskId { get; protected set; }

	public ScraperTask ScraperTask { get; protected set; } = null!;

	public string Email { get; protected set; } = null!;

	protected ScraperTaskRecipient()
	{
	}

	public ScraperTaskRecipient(string email)
	{
		Email = email;
	}

	public void SetScraperTask(ScraperTask scraperTask)
	{
		ScraperTaskId = scraperTask.Id;
		ScraperTask = scraperTask;
	}
}