namespace RealityScraper.SharedKernel;

public interface IDomainEventHandler<in T>
	where T : IDomainEvent
{
	Task HandleAsync(T domainEvent, CancellationToken cancellationToken);
}