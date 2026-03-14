namespace RealityScraper.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
	void CollectEvents();

	Task DispatchEventsAsync(CancellationToken cancellationToken);
}