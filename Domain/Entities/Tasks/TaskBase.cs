using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Tasks;

public abstract class TaskBase : AggregateRoot
{
	public string Name { get; protected set; } = null!;

	public string CronExpression { get; protected set; } = null!;

	public bool Enabled { get; protected set; } = true;

	public DateTimeOffset CreatedAt { get; protected set; }

	public DateTimeOffset? LastRunAt { get; protected set; }

	public DateTimeOffset? NextRunAt { get; protected set; }

	public string? LastRunLog { get; protected set; }

	public bool? LastRunSucceeded { get; protected set; }

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

	public void SetLastRunLog(string? log)
	{
		LastRunLog = log;
	}

	public void SetLastRunSucceeded(bool? succeeded)
	{
		LastRunSucceeded = succeeded;
	}
}