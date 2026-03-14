using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ScraperTasks.EventHandlers;

internal sealed class ScraperTaskChangedEventHandler :
	IDomainEventHandler<ScraperTaskCreatedEvent>,
	IDomainEventHandler<ScraperTaskUpdatedEvent>,
	IDomainEventHandler<ScraperTaskDeletedEvent>
{
	private readonly ISchedulerRefreshSignal schedulerRefreshSignal;
	private readonly ILogger<ScraperTaskChangedEventHandler> logger;

	public ScraperTaskChangedEventHandler(
		ISchedulerRefreshSignal schedulerRefreshSignal,
		ILogger<ScraperTaskChangedEventHandler> logger)
	{
		this.schedulerRefreshSignal = schedulerRefreshSignal;
		this.logger = logger;
	}

	public Task HandleAsync(ScraperTaskCreatedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ScraperTask created ({ScraperTaskId}), requesting scheduler refresh", domainEvent.ScraperTaskId);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}

	public Task HandleAsync(ScraperTaskUpdatedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ScraperTask updated ({ScraperTaskId}), requesting scheduler refresh", domainEvent.ScraperTaskId);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}

	public Task HandleAsync(ScraperTaskDeletedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ScraperTask deleted ({ScraperTaskId} '{Name}'), requesting scheduler refresh", domainEvent.ScraperTaskId, domainEvent.Name);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}
}