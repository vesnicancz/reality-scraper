namespace RealityScraper.Domain.Entities.Tasks;

public class ScraperTask : TaskBase
{
	private List<ScraperTaskRecipient> recipients = new List<ScraperTaskRecipient>();
	public IReadOnlyList<ScraperTaskRecipient> Recipients => recipients;

	private List<ScraperTaskTarget> targets = new List<ScraperTaskTarget>();
	public IReadOnlyList<ScraperTaskTarget> Targets => targets;

	protected ScraperTask()
	{
	}

	public ScraperTask(string name, string cronExpression, bool enabled, DateTimeOffset createdAt, DateTimeOffset? nextRunAt)
	{
		Name = name;
		CronExpression = cronExpression;
		Enabled = enabled;
		CreatedAt = createdAt;
		NextRunAt = nextRunAt;
	}

	public void SetNextRunAt(DateTimeOffset? nextRunAt)
	{
		NextRunAt = nextRunAt;
	}

	public void SetLastRunAt(DateTimeOffset lastRunAt)
	{
		LastRunAt = lastRunAt;
	}

	public void SetEnabled(bool enabled)
	{
		Enabled = enabled;
	}

	public void SetCronExpression(string cronExpression)
	{
		CronExpression = cronExpression;
	}

	public void SetName(string name)
	{
		Name = name;
	}

	public void AddRecipient(ScraperTaskRecipient recipient)
	{
		recipient.SetScraperTask(this);
		recipients.Add(recipient);
	}

	public void RemoveRecipient(ScraperTaskRecipient recipient)
	{
		recipients.Remove(recipient);
	}

	public void AddTarget(ScraperTaskTarget target)
	{
		target.SetScraperTask(this);
		targets.Add(target);
	}

	public void RemoveTarget(ScraperTaskTarget target)
	{
		targets.Remove(target);
	}
}