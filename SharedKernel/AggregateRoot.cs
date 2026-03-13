namespace RealityScraper.SharedKernel;

public abstract class AggregateRoot : Entity
{
	private readonly List<IDomainEvent> domainEvents = [];

	public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

	public void ClearDomainEvents()
	{
		domainEvents.Clear();
	}

	public void RaiseDomainEvents(IDomainEvent domainEvent)
	{
		domainEvents.Add(domainEvent);
	}
}