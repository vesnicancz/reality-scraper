namespace RealityScraper.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
	Task DispatchEventsAsync(CancellationToken cancellationToken);
}