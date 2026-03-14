using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.Events;

internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
	private readonly IDbContext dbContext;
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<DomainEventDispatcher> logger;
	private readonly List<IDomainEvent> pendingEvents = [];

	public DomainEventDispatcher(IDbContext dbContext, IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
	{
		this.dbContext = dbContext;
		this.serviceProvider = serviceProvider;
		this.logger = logger;
	}

	public void CollectEvents()
	{
		var aggregateRoots = dbContext.ChangeTracker
			.Entries<AggregateRoot>()
			.Where(e => e.Entity.DomainEvents.Count > 0)
			.Select(e => e.Entity)
			.ToList();

		pendingEvents.AddRange(aggregateRoots.SelectMany(a => a.DomainEvents));

		foreach (var aggregateRoot in aggregateRoots)
		{
			aggregateRoot.ClearDomainEvents();
		}
	}

	public async Task DispatchEventsAsync(CancellationToken cancellationToken)
	{
		var eventsToDispatch = pendingEvents.ToList();
		pendingEvents.Clear();

		foreach (var domainEvent in eventsToDispatch)
		{
			await DispatchEventAsync(domainEvent, cancellationToken);
		}
	}

	private async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
	{
		var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
		var handlers = serviceProvider.GetServices(handlerType);

		foreach (var handler in handlers)
		{
			var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
			if (method != null)
			{
				try
				{
					await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error dispatching domain event {EventType} to handler {HandlerType}", domainEvent.GetType().Name, handler!.GetType().Name);
					throw;
				}
			}
		}
	}
}