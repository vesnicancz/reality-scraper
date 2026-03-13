using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Abstractions.Events;
using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.Events;

internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
	private readonly IDbContext dbContext;
	private readonly IServiceProvider serviceProvider;

	public DomainEventDispatcher(IDbContext dbContext, IServiceProvider serviceProvider)
	{
		this.dbContext = dbContext;
		this.serviceProvider = serviceProvider;
	}

	public async Task DispatchEventsAsync(CancellationToken cancellationToken)
	{
		var aggregateRoots = dbContext.ChangeTracker
			.Entries<AggregateRoot>()
			.Where(e => e.Entity.DomainEvents.Count > 0)
			.Select(e => e.Entity)
			.ToList();

		var domainEvents = aggregateRoots
			.SelectMany(a => a.DomainEvents)
			.ToList();

		foreach (var aggregateRoot in aggregateRoots)
		{
			aggregateRoot.ClearDomainEvents();
		}

		foreach (var domainEvent in domainEvents)
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
				await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
			}
		}
	}
}