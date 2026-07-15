using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.EventHandlers;

internal sealed class ReportTaskChangedEventHandler :
	IDomainEventHandler<ReportTaskCreatedEvent>,
	IDomainEventHandler<ReportTaskUpdatedEvent>,
	IDomainEventHandler<ReportTaskDeletedEvent>
{
	private readonly ISchedulerRefreshSignal schedulerRefreshSignal;
	private readonly ILogger<ReportTaskChangedEventHandler> logger;

	public ReportTaskChangedEventHandler(
		ISchedulerRefreshSignal schedulerRefreshSignal,
		ILogger<ReportTaskChangedEventHandler> logger)
	{
		this.schedulerRefreshSignal = schedulerRefreshSignal;
		this.logger = logger;
	}

	public Task HandleAsync(ReportTaskCreatedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ReportTask created ({ReportTaskId}), requesting scheduler refresh", domainEvent.ReportTaskId);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}

	public Task HandleAsync(ReportTaskUpdatedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ReportTask updated ({ReportTaskId}), requesting scheduler refresh", domainEvent.ReportTaskId);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}

	public Task HandleAsync(ReportTaskDeletedEvent domainEvent, CancellationToken cancellationToken)
	{
		logger.LogInformation("ReportTask deleted ({ReportTaskId} '{Name}'), requesting scheduler refresh", domainEvent.ReportTaskId, domainEvent.Name);
		schedulerRefreshSignal.RequestRefresh();
		return Task.CompletedTask;
	}
}
