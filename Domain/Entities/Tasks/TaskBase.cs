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
}