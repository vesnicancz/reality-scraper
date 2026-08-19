using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.Domain.Events;

namespace RealityScraper.Domain.Tests.Entities.Tasks;

public class ScraperTaskTests
{
	private static ScraperTask CreateTask()
	{
		return new ScraperTask(
			name: "Denní sken",
			cronExpression: "0 6 * * *",
			enabled: true,
			createdAt: new DateTimeOffset(2026, 1, 15, 6, 0, 0, TimeSpan.Zero),
			nextRunAt: new DateTimeOffset(2026, 1, 16, 6, 0, 0, TimeSpan.Zero));
	}

	[Fact]
	public void Constructor_SetsProvidedValues()
	{
		var createdAt = new DateTimeOffset(2026, 1, 15, 6, 0, 0, TimeSpan.Zero);
		var nextRunAt = new DateTimeOffset(2026, 1, 16, 6, 0, 0, TimeSpan.Zero);

		var task = new ScraperTask("Denní sken", "0 6 * * *", enabled: true, createdAt, nextRunAt);

		Assert.Equal("Denní sken", task.Name);
		Assert.Equal("0 6 * * *", task.CronExpression);
		Assert.True(task.Enabled);
		Assert.Equal(createdAt, task.CreatedAt);
		Assert.Equal(nextRunAt, task.NextRunAt);
		Assert.Null(task.LastRunAt);
	}

	[Fact]
	public void AddRecipient_SetsBackReferenceToTask()
	{
		var task = CreateTask();
		task.Id = Guid.NewGuid();
		var recipient = new ScraperTaskRecipient("info@example.com");

		task.AddRecipient(recipient);

		Assert.Same(task, recipient.ScraperTask);
		Assert.Equal(task.Id, recipient.ScraperTaskId);
		Assert.Equal(recipient, Assert.Single(task.Recipients));
	}

	[Fact]
	public void AddTarget_SetsBackReferenceToTask()
	{
		var task = CreateTask();
		task.Id = Guid.NewGuid();
		var target = new ScraperTaskTarget(ScrapersEnum.SReality, "https://www.sreality.cz/hledani/prodej/domy");

		task.AddTarget(target);

		Assert.Same(task, target.ScraperTask);
		Assert.Equal(task.Id, target.ScraperTaskId);
		Assert.Equal(ScrapersEnum.SReality, Assert.Single(task.Targets).ScraperType);
	}

	[Fact]
	public void RemoveRecipient_RemovesOnlyGivenRecipient()
	{
		var task = CreateTask();
		var first = new ScraperTaskRecipient("prvni@example.com");
		var second = new ScraperTaskRecipient("druhy@example.com");
		task.AddRecipient(first);
		task.AddRecipient(second);

		task.RemoveRecipient(first);

		Assert.Equal("druhy@example.com", Assert.Single(task.Recipients).Email);
	}

	[Fact]
	public void RemoveTarget_RemovesOnlyGivenTarget()
	{
		var task = CreateTask();
		var first = new ScraperTaskTarget(ScrapersEnum.SReality, "https://www.sreality.cz/a");
		var second = new ScraperTaskTarget(ScrapersEnum.RealityIdnes, "https://reality.idnes.cz/b");
		task.AddTarget(first);
		task.AddTarget(second);

		task.RemoveTarget(first);

		Assert.Equal(ScrapersEnum.RealityIdnes, Assert.Single(task.Targets).ScraperType);
	}

	[Fact]
	public void Setters_OverwritePlannedRunState()
	{
		var task = CreateTask();
		var lastRunAt = new DateTimeOffset(2026, 1, 16, 6, 0, 0, TimeSpan.Zero);

		task.SetLastRunAt(lastRunAt);
		task.SetNextRunAt(null);
		task.SetEnabled(false);
		task.SetLastRunSucceeded(false);
		task.SetLastRunLog("timeout");

		Assert.Equal(lastRunAt, task.LastRunAt);
		Assert.Null(task.NextRunAt);
		Assert.False(task.Enabled);
		Assert.False(task.LastRunSucceeded);
		Assert.Equal("timeout", task.LastRunLog);
	}

	[Fact]
	public void RaiseDomainEvent_CollectsEventsUntilCleared()
	{
		var task = CreateTask();
		task.Id = Guid.NewGuid();
		var createdEvent = new ScraperTaskCreatedEvent(task.Id);

		task.RaiseDomainEvent(createdEvent);

		Assert.Same(createdEvent, Assert.Single(task.DomainEvents));

		task.ClearDomainEvents();

		Assert.Empty(task.DomainEvents);
	}
}
